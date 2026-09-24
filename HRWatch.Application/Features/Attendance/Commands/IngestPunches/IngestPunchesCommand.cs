using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Entities;
using HRWatch.Domain.Enums;
using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Attendance.Commands.IngestPunches;

public record IngestPunchesRequestDto(
    DateOnly TargetDate,
    List<CosecPunchRecord> Punches,
    bool EvaluateAttendance = true
);

public record IngestPunchesCommand(
    DateOnly TargetDate,
    List<CosecPunchRecord> Punches,
    bool EvaluateAttendance = true,
    string TriggeredBy = "OfficeRelay"
) : ICommand<Result<IngestPunchesResult>>;

public record IngestPunchesResult(
    DateOnly EvaluationDate,
    int TotalPunchesIngested,
    int TotalActiveEmployees,
    int PresentCount,
    int AbsentCount,
    int LeaveCount,
    int WfhCount,
    int ExceptionCount,
    DateTime IngestedAt
);

public class IngestPunchesCommandHandler : ICommandHandler<IngestPunchesCommand, Result<IngestPunchesResult>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICg1ApiClient _cg1Client;
    private readonly ILogger<IngestPunchesCommandHandler> _logger;

    public IngestPunchesCommandHandler(
        IApplicationDbContext dbContext,
        ICg1ApiClient cg1Client,
        ILogger<IngestPunchesCommandHandler> logger)
    {
        _dbContext = dbContext;
        _cg1Client = cg1Client;
        _logger = logger;
    }

    public async Task<Result<IngestPunchesResult>> HandleAsync(IngestPunchesCommand command, CancellationToken cancellationToken = default)
    {
        var targetDate = command.TargetDate;
        _logger.LogInformation("Starting IngestPunches for Date: {Date} with {Count} punches triggered by {TriggeredBy}",
            targetDate, command.Punches?.Count ?? 0, command.TriggeredBy);

        // 1. Get or create Active Policy
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
            return Result<IngestPunchesResult>.Failure("No active India employees found. Please sync employees first.", "NO_ACTIVE_EMPLOYEES");
        }

        var empCodeToIdMap = activeEmployees.ToDictionary(e => e.EmployeeCode.Trim().ToUpperInvariant(), e => e.Id);

        // 3. Save Ingested Punches to DailyPunchLogs
        var existingPunchLogs = await _dbContext.DailyPunchLogs
            .Where(p => p.PunchDate == targetDate)
            .Select(p => p.RawLogIndex)
            .Where(idx => idx != null)
            .ToHashSetAsync(cancellationToken);

        var newPunchLogs = new List<DailyPunchLog>();
        var incomingPunches = command.Punches ?? [];

        foreach (var pr in incomingPunches)
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
                DeviceName = pr.DeviceName ?? "Office Relay",
                EntryExitType = pr.EntryExitType,
                RawLogIndex = pr.IndexNo,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (newPunchLogs.Count > 0)
        {
            await _dbContext.DailyPunchLogs.AddRangeAsync(newPunchLogs, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!command.EvaluateAttendance)
        {
            return Result<IngestPunchesResult>.Success(new IngestPunchesResult(
                targetDate, newPunchLogs.Count, activeEmployees.Count, 0, 0, 0, 0, 0, DateTime.UtcNow));
        }

        // 4. Handle Weekend Check
        var dayOfWeek = targetDate.DayOfWeek;
        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            var weekendAttendances = activeEmployees.Select(e => new DailyAttendance
            {
                EmployeeId = e.Id,
                Date = targetDate,
                Status = AttendanceStatus.WO,
                RuleVersionId = activePolicy.Id
            }).ToList();

            await UpsertAttendanceRecordsAsync(weekendAttendances, targetDate, cancellationToken);

            return Result<IngestPunchesResult>.Success(new IngestPunchesResult(
                targetDate, newPunchLogs.Count, activeEmployees.Count, 0, 0, 0, 0, 0, DateTime.UtcNow));
        }

        // 5. Evaluate Attendance using Ingested Punches
        var allPunchesForDate = await _dbContext.DailyPunchLogs
            .Where(p => p.PunchDate == targetDate && !string.IsNullOrWhiteSpace(p.EmployeeCode))
            .Select(p => p.EmployeeCode.Trim().ToUpperInvariant())
            .Distinct()
            .ToListAsync(cancellationToken);

        var presentPunchCodes = allPunchesForDate.ToHashSet();

        // Active Exceptions
        var activeExceptions = await _dbContext.EmployeeExceptions
            .Where(e => e.IsActive && targetDate >= e.FromDate && targetDate <= e.ToDate)
            .ToDictionaryAsync(e => e.EmployeeId, cancellationToken);

        var potentialViolators = new List<Employee>();
        var attendanceToUpsert = new List<DailyAttendance>();

        int presentCount = 0;
        int leaveCount = 0;
        int wfhCount = 0;
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

        // Check CG1 Leaves for non-punched employees
        var leavesMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (potentialViolators.Count > 0)
        {
            try
            {
                var violatorEmails = potentialViolators.Select(e => e.Email).ToList();
                var leaveResponses = await _cg1Client.GetLeavesByEmailsAsync(violatorEmails, targetDate, cancellationToken);
                foreach (var resp in leaveResponses)
                {
                    if (string.IsNullOrWhiteSpace(resp.Email) || resp.Leave == null) continue;
                    if (resp.Leave.Any(l => string.Equals(l, "H", StringComparison.OrdinalIgnoreCase)))
                    {
                        leavesMap[resp.Email.Trim().ToLowerInvariant()] = "H";
                    }
                    else if (resp.Leave.Any(l => string.Equals(l, "L", StringComparison.OrdinalIgnoreCase)))
                    {
                        leavesMap[resp.Email.Trim().ToLowerInvariant()] = "L";
                    }
                    else if (resp.Leave.Any(l => string.Equals(l, "W", StringComparison.OrdinalIgnoreCase)))
                    {
                        leavesMap[resp.Email.Trim().ToLowerInvariant()] = "W";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("CG1 Leave API call note: {Message}", ex.Message);
            }
        }

        foreach (var emp in potentialViolators)
        {
            var emailLower = emp.Email.Trim().ToLowerInvariant();
            if (leavesMap.TryGetValue(emailLower, out var leaveCode))
            {
                if (leaveCode == "H")
                {
                    attendanceToUpsert.Add(new DailyAttendance
                    {
                        EmployeeId = emp.Id,
                        Date = targetDate,
                        Status = AttendanceStatus.H,
                        LeaveType = "Public Holiday",
                        RuleVersionId = activePolicy.Id
                    });
                    leaveCount++;
                    continue;
                }
                if (leaveCode == "L")
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
                    continue;
                }
                if (leaveCode == "W")
                {
                    attendanceToUpsert.Add(new DailyAttendance
                    {
                        EmployeeId = emp.Id,
                        Date = targetDate,
                        Status = AttendanceStatus.W,
                        LeaveType = "Work From Home",
                        RuleVersionId = activePolicy.Id
                    });
                    wfhCount++;
                    continue;
                }
            }

            if (activeExceptions.TryGetValue(emp.Id, out var exRecord))
            {
                attendanceToUpsert.Add(new DailyAttendance
                {
                    EmployeeId = emp.Id,
                    Date = targetDate,
                    Status = AttendanceStatus.E,
                    LeaveType = $"Exception: {exRecord.Reason}",
                    RuleVersionId = activePolicy.Id
                });
                exceptionCount++;
                continue;
            }

            // Absent
            attendanceToUpsert.Add(new DailyAttendance
            {
                EmployeeId = emp.Id,
                Date = targetDate,
                Status = AttendanceStatus.A,
                RuleVersionId = activePolicy.Id
            });
            absentCount++;
        }

        await UpsertAttendanceRecordsAsync(attendanceToUpsert, targetDate, cancellationToken);

        _logger.LogInformation("Ingest attendance completed for {Date}. Present: {P}, Leave: {L}, WFH: {W}, Exception: {E}, Absent: {A}",
            targetDate, presentCount, leaveCount, wfhCount, exceptionCount, absentCount);

        return Result<IngestPunchesResult>.Success(new IngestPunchesResult(
            targetDate, newPunchLogs.Count, activeEmployees.Count, presentCount, absentCount, leaveCount, wfhCount, exceptionCount, DateTime.UtcNow));
    }

    private async Task UpsertAttendanceRecordsAsync(List<DailyAttendance> records, DateOnly date, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.DailyAttendances
            .Where(a => a.Date == date)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _dbContext.DailyAttendances.RemoveRange(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await _dbContext.DailyAttendances.AddRangeAsync(records, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
