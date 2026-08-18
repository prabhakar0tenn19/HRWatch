using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Entities;
using HRWatch.Domain.Enums;
using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Attendance.Commands.EvaluateDailyAttendance;

public record EvaluateDailyAttendanceCommand(DateOnly? TargetDate = null, string TriggeredBy = "System")
    : ICommand<Result<EvaluateDailyAttendanceResult>>;

public record EvaluateDailyAttendanceResult(
    DateOnly EvaluationDate,
    int TotalActiveEmployees,
    int PresentCount,
    int LeaveCount,
    int ExceptionCount,
    int AbsentCount,
    int WeekendOrHolidayCount,
    DateTime EvaluatedAt);

public class EvaluateDailyAttendanceCommandHandler : ICommandHandler<EvaluateDailyAttendanceCommand, Result<EvaluateDailyAttendanceResult>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICosecBiometricApiClient _cosecClient;
    private readonly ICg1ApiClient _cg1Client;
    private readonly ILogger<EvaluateDailyAttendanceCommandHandler> _logger;

    public EvaluateDailyAttendanceCommandHandler(
        IApplicationDbContext dbContext,
        ICosecBiometricApiClient cosecClient,
        ICg1ApiClient cg1Client,
        ILogger<EvaluateDailyAttendanceCommandHandler> logger)
    {
        _dbContext = dbContext;
        _cosecClient = cosecClient;
        _cg1Client = cg1Client;
        _logger = logger;
    }

    public async Task<Result<EvaluateDailyAttendanceResult>> HandleAsync(EvaluateDailyAttendanceCommand command, CancellationToken cancellationToken = default)
    {
        var targetDate = command.TargetDate ?? DateOnly.FromDateTime(DateTime.Today);
        _logger.LogInformation("Starting Daily Attendance Evaluation for Date: {Date} triggered by {TriggeredBy}",
            targetDate, command.TriggeredBy);

        // 1. Get Active Policy
        var activePolicy = await _dbContext.Policies.FirstOrDefaultAsync(p => p.IsActive, cancellationToken);
        if (activePolicy == null)
        {
            activePolicy = new Policy
            {
                Version = 1,
                PolicyName = "Default CG India WFO Policy",
                RulesJson = "{\"MinWfoDaysPerWeek\":{\"SDE\":5,\"Consultant\":5,\"Intern\":5,\"Associate\":3,\"Manager\":3,\"Principal\":3,\"Bench\":5},\"DefaultRequiredDays\":5}",
                EffectiveFrom = DateOnly.FromDateTime(DateTime.Today.AddYears(-1)),
                IsActive = true,
                CreatedBy = "System"
            };
            await _dbContext.Policies.AddAsync(activePolicy, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 2. Fetch Active India Employees
        var activeEmployees = await _dbContext.Employees
            .Where(e => e.IsActive && e.Location == "India")
            .ToListAsync(cancellationToken);

        if (activeEmployees.Count == 0)
        {
            _logger.LogWarning("No active India employees found in database. Run sync first.");
            return Result<EvaluateDailyAttendanceResult>.Success(
                new EvaluateDailyAttendanceResult(targetDate, 0, 0, 0, 0, 0, 0, DateTime.UtcNow));
        }

        // 3. Handle Weekend Check
        var dayOfWeek = targetDate.DayOfWeek;
        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            _logger.LogInformation("Date {Date} is Weekend ({DayOfWeek}). Marking all as WeekendOff.", targetDate, dayOfWeek);
            await ProcessWeekendOffAsync(activeEmployees, targetDate, activePolicy.Id, cancellationToken);

            return Result<EvaluateDailyAttendanceResult>.Success(
                new EvaluateDailyAttendanceResult(targetDate, activeEmployees.Count, 0, 0, 0, 0, activeEmployees.Count, DateTime.UtcNow));
        }

        // 4. Fetch Biometric Punches from Matrix COSEC API and save to DailyPunchLogs
        var targetDateTime = targetDate.ToDateTime(TimeOnly.MinValue);
        var punchRecords = await _cosecClient.GetPunchesForDateRangeAsync(targetDateTime, targetDateTime, cancellationToken);
        _logger.LogInformation("COSEC Biometric returned {Count} raw punch records for {Date}",
            punchRecords.Count, targetDate);

        var empCodeToIdMap = activeEmployees.ToDictionary(e => e.EmployeeCode.Trim().ToUpperInvariant(), e => e.Id);
        var existingPunchLogs = await _dbContext.DailyPunchLogs
            .Where(p => p.PunchDate == targetDate)
            .Select(p => p.RawLogIndex)
            .Where(idx => idx != null)
            .ToHashSetAsync(cancellationToken);

        var newPunchLogs = new List<DailyPunchLog>();
        foreach (var pr in punchRecords)
        {
            if (pr.IndexNo != null && existingPunchLogs.Contains(pr.IndexNo))
                continue;

            empCodeToIdMap.TryGetValue(pr.EmployeeCode.Trim().ToUpperInvariant(), out var matchedEmpId);
            newPunchLogs.Add(new DailyPunchLog
            {
                EmployeeCode = pr.EmployeeCode.Trim(),
                EmployeeId = matchedEmpId != Guid.Empty ? matchedEmpId : null,
                PunchDate = pr.PunchDate,
                PunchTime = pr.PunchTime,
                DeviceName = pr.DeviceName,
                EntryExitType = pr.EntryExitType,
                RawLogIndex = pr.IndexNo,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (newPunchLogs.Count > 0)
        {
            await _dbContext.DailyPunchLogs.AddRangeAsync(newPunchLogs, cancellationToken);
        }

        var presentPunchCodes = punchRecords
            .Where(r => r.PunchDate == targetDate && !string.IsNullOrWhiteSpace(r.EmployeeCode))
            .Select(r => r.EmployeeCode.Trim().ToUpperInvariant())
            .ToHashSet();

        _logger.LogInformation("Found {Count} distinct present punch codes for {Date}",
            presentPunchCodes.Count, targetDate);

        // 5. Fetch Active Exceptions for this date
        var activeExceptions = await _dbContext.EmployeeExceptions
            .Where(e => e.IsActive && targetDate >= e.FromDate && targetDate <= e.ToDate)
            .ToDictionaryAsync(e => e.EmployeeId, cancellationToken);

        // 6. Separate Present Employees vs Potential Violators
        var potentialViolators = new List<Employee>();
        var attendanceToUpsert = new List<DailyAttendance>();

        int presentCount = 0;
        int leaveCount = 0;
        int exceptionCount = 0;
        int absentCount = 0;

        foreach (var emp in activeEmployees)
        {
            var empCodeUpper = emp.EmployeeCode.Trim().ToUpperInvariant();
            if (presentPunchCodes.Contains(empCodeUpper))
            {
                attendanceToUpsert.Add(new DailyAttendance
                {
                    EmployeeId = emp.Id,
                    Date = targetDate,
                    Status = AttendanceStatus.P,
                    RuleVersionId = activePolicy.Id
                });
                presentCount++;
            }
            else
            {
                potentialViolators.Add(emp);
            }
        }

        // 7. Verify Potential Violators with CG1 Leave by-emails API
        var leavesMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        if (potentialViolators.Count > 0)
        {
            var violatorEmails = potentialViolators.Select(e => e.Email).ToList();
            var leaveResponses = await _cg1Client.GetLeavesByEmailsAsync(violatorEmails, targetDate, cancellationToken);

            foreach (var resp in leaveResponses)
            {
                if (string.IsNullOrWhiteSpace(resp.Email)) continue;

                // CRITICAL RULE: ONLY count as leave if leave array has "L".
                // Ignore "P" from CG1 API since physical punch was missing.
                bool isOnApprovedLeave = resp.Leave != null && resp.Leave.Any(l => string.Equals(l, "L", StringComparison.OrdinalIgnoreCase));
                leavesMap[resp.Email.Trim()] = isOnApprovedLeave;
            }
        }

        // 8. Process each Potential Violator
        foreach (var emp in potentialViolators)
        {
            leavesMap.TryGetValue(emp.Email, out bool isLeave);

            if (isLeave)
            {
                attendanceToUpsert.Add(new DailyAttendance
                {
                    EmployeeId = emp.Id,
                    Date = targetDate,
                    Status = AttendanceStatus.L,
                    LeaveType = "Approved Leave",
                    RuleVersionId = activePolicy.Id
                });
                leaveCount++;
            }
            else if (activeExceptions.TryGetValue(emp.Id, out var ex))
            {
                attendanceToUpsert.Add(new DailyAttendance
                {
                    EmployeeId = emp.Id,
                    Date = targetDate,
                    Status = AttendanceStatus.E,
                    LeaveType = $"Exception: {ex.Reason}",
                    RuleVersionId = activePolicy.Id
                });
                exceptionCount++;
            }
            else
            {
                // ABSENT / VIOLATOR
                attendanceToUpsert.Add(new DailyAttendance
                {
                    EmployeeId = emp.Id,
                    Date = targetDate,
                    Status = AttendanceStatus.A,
                    RuleVersionId = activePolicy.Id
                });
                absentCount++;
            }
        }

        // 9. Batch Upsert into DailyAttendance table
        var existingAttendances = await _dbContext.DailyAttendances
            .Where(a => a.Date == targetDate)
            .ToDictionaryAsync(a => a.EmployeeId, cancellationToken);

        foreach (var att in attendanceToUpsert)
        {
            if (existingAttendances.TryGetValue(att.EmployeeId, out var existing))
            {
                existing.Status = att.Status;
                existing.LeaveType = att.LeaveType;
                existing.RuleVersionId = att.RuleVersionId;
                existing.UpdatedAt = DateTime.UtcNow;
                _dbContext.DailyAttendances.Update(existing);
            }
            else
            {
                att.CreatedAt = DateTime.UtcNow;
                await _dbContext.DailyAttendances.AddAsync(att, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Daily Evaluation completed for {Date}. Present: {P}, Leave: {L}, Exception: {E}, Absent: {A}",
            targetDate, presentCount, leaveCount, exceptionCount, absentCount);

        return Result<EvaluateDailyAttendanceResult>.Success(new EvaluateDailyAttendanceResult(
            targetDate,
            activeEmployees.Count,
            presentCount,
            leaveCount,
            exceptionCount,
            absentCount,
            0,
            DateTime.UtcNow));
    }

    private async Task ProcessWeekendOffAsync(List<Employee> employees, DateOnly targetDate, Guid policyId, CancellationToken cancellationToken)
    {
        var existingAttendances = await _dbContext.DailyAttendances
            .Where(a => a.Date == targetDate)
            .ToDictionaryAsync(a => a.EmployeeId, cancellationToken);

        foreach (var emp in employees)
        {
            if (existingAttendances.TryGetValue(emp.Id, out var existing))
            {
                existing.Status = AttendanceStatus.WO;
                existing.LeaveType = "Weekend Off";
                existing.UpdatedAt = DateTime.UtcNow;
                _dbContext.DailyAttendances.Update(existing);
            }
            else
            {
                await _dbContext.DailyAttendances.AddAsync(new DailyAttendance
                {
                    EmployeeId = emp.Id,
                    Date = targetDate,
                    Status = AttendanceStatus.WO,
                    LeaveType = "Weekend Off",
                    RuleVersionId = policyId,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
