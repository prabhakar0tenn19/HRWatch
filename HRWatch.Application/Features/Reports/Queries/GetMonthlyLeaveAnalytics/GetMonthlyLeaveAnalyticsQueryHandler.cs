using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Reports.Queries.GetMonthlyLeaveAnalytics;

public class GetMonthlyLeaveAnalyticsQueryHandler : IQueryHandler<GetMonthlyLeaveAnalyticsQuery, IReadOnlyList<MonthlyLeaveAnalyticsDto>>
{
    private readonly IWeeklyAttendanceRepository _weeklyAttendanceRepository;

    public GetMonthlyLeaveAnalyticsQueryHandler(IWeeklyAttendanceRepository weeklyAttendanceRepository)
    {
        _weeklyAttendanceRepository = weeklyAttendanceRepository;
    }

    public async Task<Result<IReadOnlyList<MonthlyLeaveAnalyticsDto>>> HandleAsync(
        GetMonthlyLeaveAnalyticsQuery query,
        CancellationToken cancellationToken = default)
    {
        var records = await _weeklyAttendanceRepository.GetByMonthAsync(query.Year, query.Month, cancellationToken);

        var grouped = records
            .GroupBy(r => r.EmployeeId)
            .Select(g =>
            {
                var first = g.First();
                var emp = first.Employee;

                int totalPresent = g.Sum(r => r.PresentCount);
                int totalLeaves = g.Sum(r => r.LeaveCount);
                int totalTracked = totalPresent + totalLeaves;
                double percentage = totalTracked > 0 ? Math.Round((double)totalLeaves / totalTracked * 100, 2) : 0;

                return new MonthlyLeaveAnalyticsDto(
                    g.Key,
                    emp?.FullName ?? "Unknown",
                    emp?.Email ?? string.Empty,
                    emp?.Designation?.Title ?? "N/A",
                    emp?.Department ?? "General",
                    query.Year,
                    query.Month,
                    totalPresent,
                    totalLeaves,
                    percentage);
            })
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Designation))
        {
            grouped = grouped.Where(x => string.Equals(x.Designation, query.Designation, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            grouped = grouped.Where(x => x.EmployeeName.ToLower().Contains(term) || x.Email.ToLower().Contains(term));
        }

        grouped = query.SortByLeaveCountDesc
            ? grouped.OrderByDescending(x => x.TotalLeaveDays)
            : grouped.OrderBy(x => x.TotalLeaveDays);

        return Result<IReadOnlyList<MonthlyLeaveAnalyticsDto>>.Success(grouped.ToList());
    }
}
