using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Reports.Queries.GetMonthlyLeaveAnalytics;

public record GetMonthlyLeaveAnalyticsQuery(
    int Year,
    int Month,
    string? Designation = null,
    string? SearchTerm = null,
    bool SortByLeaveCountDesc = true
) : IQuery<IReadOnlyList<MonthlyLeaveAnalyticsDto>>;

public record MonthlyLeaveAnalyticsDto(
    Guid EmployeeId,
    string EmployeeName,
    string Email,
    string Designation,
    string Department,
    int Year,
    int Month,
    int TotalPresentDays,
    int TotalLeaveDays,
    double LeavePercentage);
