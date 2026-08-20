using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Enums;
using HRWatch.Domain.Services;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Application.Features.Violations.Queries.GetWeeklyViolators;

public record GetWeeklyViolatorsQuery(
    DateOnly? WeekStartDate = null,
    string? Designation = null,
    string? SearchTerm = null
) : IQuery<Result<IReadOnlyList<WeeklyViolatorDto>>>;

public record WeeklyViolatorDto(
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    string Email,
    string Designation,
    bool IsDeployed,
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    int RequiredDays,
    int ActualPresentDays,
    int LeaveDays,
    int WfhDays,
    int AbsentDays,
    int ShortfallDays,
    string Severity);

public class GetWeeklyViolatorsQueryHandler : IQueryHandler<GetWeeklyViolatorsQuery, Result<IReadOnlyList<WeeklyViolatorDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IWfoEvaluationService _wfoService;

    public GetWeeklyViolatorsQueryHandler(IApplicationDbContext dbContext, IWfoEvaluationService wfoService)
    {
        _dbContext = dbContext;
        _wfoService = wfoService;
    }

    public async Task<Result<IReadOnlyList<WeeklyViolatorDto>>> HandleAsync(GetWeeklyViolatorsQuery query, CancellationToken cancellationToken = default)
    {
        var weekStart = query.WeekStartDate ?? GetCurrentMonday(DateOnly.FromDateTime(DateTime.Today));
        var weekEnd = weekStart.AddDays(4);

        var activePolicy = await _dbContext.Policies.FirstOrDefaultAsync(p => p.IsActive, cancellationToken);
        var rulesJson = activePolicy?.RulesJson;

        var empQuery = _dbContext.Employees
            .Where(e => e.IsActive && e.Location == "India")
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Designation))
        {
            var desig = query.Designation.Trim();
            empQuery = empQuery.Where(e => EF.Functions.Like(e.Designation, $"%{desig}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            empQuery = empQuery.Where(e => EF.Functions.Like(e.FullName, $"%{term}%") ||
                                           EF.Functions.Like(e.Email, $"%{term}%") ||
                                           EF.Functions.Like(e.EmployeeCode, $"%{term}%"));
        }

        var employees = await empQuery.ToListAsync(cancellationToken);
        if (employees.Count == 0)
        {
            return Result<IReadOnlyList<WeeklyViolatorDto>>.Success([]);
        }

        var empIds = employees.Select(e => e.Id).ToList();

        var attendances = await _dbContext.DailyAttendances
            .Where(a => empIds.Contains(a.EmployeeId) && a.Date >= weekStart && a.Date <= weekEnd)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var attendancesByEmp = attendances
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var violators = new List<WeeklyViolatorDto>();

        foreach (var emp in employees)
        {
            attendancesByEmp.TryGetValue(emp.Id, out var empAtts);
            empAtts ??= [];

            int presentDays = empAtts.Count(a => a.Status == AttendanceStatus.P);
            int leaveDays = empAtts.Count(a => a.Status == AttendanceStatus.L);
            int wfhDays = empAtts.Count(a => a.Status == AttendanceStatus.W);
            int exceptionDays = empAtts.Count(a => a.Status == AttendanceStatus.E);
            int absentDays = empAtts.Count(a => a.Status == AttendanceStatus.A);

            int requiredDays = _wfoService.GetRequiredWfoDays(emp.Designation, emp.IsDeployed, rulesJson);
            var (isViolator, shortfall, severity) = _wfoService.EvaluateWeeklyCompliance(
                presentDays, requiredDays, leaveDays, wfhDays, exceptionDays, absentDays);

            if (isViolator)
            {
                violators.Add(new WeeklyViolatorDto(
                    emp.Id,
                    emp.EmployeeCode,
                    emp.FullName,
                    emp.Email,
                    emp.Designation,
                    emp.IsDeployed,
                    weekStart,
                    weekEnd,
                    requiredDays,
                    presentDays,
                    leaveDays,
                    wfhDays,
                    absentDays,
                    shortfall,
                    severity?.ToString() ?? "Low"));
            }
        }

        var sortedViolators = violators
            .OrderByDescending(v => v.ShortfallDays)
            .ThenBy(v => v.FullName)
            .ToList();

        return Result<IReadOnlyList<WeeklyViolatorDto>>.Success(sortedViolators);
    }

    private static DateOnly GetCurrentMonday(DateOnly date)
    {
        var dayOfWeek = date.DayOfWeek;
        int diff = ((int)dayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }
}
