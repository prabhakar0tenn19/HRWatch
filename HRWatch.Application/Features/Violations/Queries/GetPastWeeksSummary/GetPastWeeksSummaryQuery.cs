using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Application.Features.Violations.Queries.GetWeeklyViolators;
using HRWatch.Domain.Entities;
using HRWatch.Domain.Enums;
using HRWatch.Domain.Services;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Application.Features.Violations.Queries.GetPastWeeksSummary;

public record TopShortfallEmployeeDto(
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    string Email,
    string Designation,
    bool IsDeployed,
    int TotalShortfallDays,
    int WeeksWithViolations);

public record WeekCardSummaryDto(
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    string WeekLabel,
    int TotalViolators,
    int CriticalViolators,
    IReadOnlyList<WeeklyViolatorDto> Violators);

public record PastWeeksSummaryResponseDto(
    int TotalWeeksEvaluated,
    IReadOnlyList<WeekCardSummaryDto> Weeks,
    IReadOnlyList<TopShortfallEmployeeDto> TopShortfallEmployees);

public record GetPastWeeksSummaryQuery(
    int WeeksCount = 4,
    string? Designation = null,
    string? SearchTerm = null) : IQuery<Result<PastWeeksSummaryResponseDto>>;

public class GetPastWeeksSummaryQueryHandler : IQueryHandler<GetPastWeeksSummaryQuery, Result<PastWeeksSummaryResponseDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IWfoEvaluationService _wfoService;

    public GetPastWeeksSummaryQueryHandler(IApplicationDbContext dbContext, IWfoEvaluationService wfoService)
    {
        _dbContext = dbContext;
        _wfoService = wfoService;
    }

    public async Task<Result<PastWeeksSummaryResponseDto>> HandleAsync(GetPastWeeksSummaryQuery query, CancellationToken cancellationToken = default)
    {
        int weeksCount = Math.Clamp(query.WeeksCount, 1, 12);

        var currentMonday = GetCurrentMonday(HRWatch.Domain.Common.IndiaDateTime.Today);
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
            return Result<PastWeeksSummaryResponseDto>.Success(
                new PastWeeksSummaryResponseDto(0, [], []));
        }

        var empIds = employees.Select(e => e.Id).ToList();

        // Calculate start date of oldest week requested
        var oldestWeekMonday = currentMonday.AddDays(-(weeksCount - 1) * 7);
        var latestWeekFriday = currentMonday.AddDays(4);

        var allAttendances = await _dbContext.DailyAttendances
            .Where(a => empIds.Contains(a.EmployeeId) && a.Date >= oldestWeekMonday && a.Date <= latestWeekFriday)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var weekCards = new List<WeekCardSummaryDto>();
        var employeeAggregates = new Dictionary<Guid, (int TotalShortfall, int WeeksCount, Employee Emp)>();

        // Process from most recent week to oldest week
        for (int i = 0; i < weeksCount; i++)
        {
            var weekStart = currentMonday.AddDays(-i * 7);
            var weekEnd = weekStart.AddDays(4);
            var weekLabel = $"{weekStart:dd MMM} - {weekEnd:dd MMM}";

            var weekAtts = allAttendances
                .Where(a => a.Date >= weekStart && a.Date <= weekEnd)
                .GroupBy(a => a.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var weekViolators = new List<WeeklyViolatorDto>();

            foreach (var emp in employees)
            {
                weekAtts.TryGetValue(emp.Id, out var empAtts);
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
                    weekViolators.Add(new WeeklyViolatorDto(
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

                    // Aggregate for Top 5 widget
                    if (!employeeAggregates.TryGetValue(emp.Id, out var agg))
                    {
                        employeeAggregates[emp.Id] = (shortfall, 1, emp);
                    }
                    else
                    {
                        employeeAggregates[emp.Id] = (agg.TotalShortfall + shortfall, agg.WeeksCount + 1, emp);
                    }
                }
            }

            var sortedWeekViolators = weekViolators
                .OrderByDescending(v => v.ShortfallDays)
                .ThenBy(v => v.FullName)
                .ToList();

            int criticalCount = sortedWeekViolators.Count(v => v.ShortfallDays >= 3 || v.Severity == "High");

            weekCards.Add(new WeekCardSummaryDto(
                weekStart,
                weekEnd,
                weekLabel,
                sortedWeekViolators.Count,
                criticalCount,
                sortedWeekViolators));
        }

        // Compute Top 5 highest shortfall employees across the evaluated weeks
        var top5 = employeeAggregates.Values
            .OrderByDescending(a => a.TotalShortfall)
            .ThenByDescending(a => a.WeeksCount)
            .ThenBy(a => a.Emp.FullName)
            .Take(5)
            .Select(a => new TopShortfallEmployeeDto(
                a.Emp.Id,
                a.Emp.EmployeeCode,
                a.Emp.FullName,
                a.Emp.Email,
                a.Emp.Designation,
                a.Emp.IsDeployed,
                a.TotalShortfall,
                a.WeeksCount))
            .ToList();

        return Result<PastWeeksSummaryResponseDto>.Success(new PastWeeksSummaryResponseDto(
            weeksCount,
            weekCards,
            top5));
    }

    private static DateOnly GetCurrentMonday(DateOnly date)
    {
        var dayOfWeek = date.DayOfWeek;
        int diff = ((int)dayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }
}
