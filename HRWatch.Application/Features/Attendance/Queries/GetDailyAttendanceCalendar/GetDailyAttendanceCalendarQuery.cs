using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Application.Features.Attendance.Queries.GetDailyAttendanceCalendar;

public record GetDailyAttendanceCalendarQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    string? Designation = null,
    string? SearchTerm = null
) : IQuery<Result<IReadOnlyList<EmployeeCalendarDto>>>;

public record EmployeeCalendarDto(
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    string Email,
    string Designation,
    bool IsDeployed,
    List<DailyAttendanceStatusDto> Days);

public record DailyAttendanceStatusDto(
    DateOnly Date,
    string DayOfWeek,
    string StatusCode, // 'P', 'L', 'E', 'A', 'WO', 'H'
    string? LeaveType);

public class GetDailyAttendanceCalendarQueryHandler : IQueryHandler<GetDailyAttendanceCalendarQuery, Result<IReadOnlyList<EmployeeCalendarDto>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetDailyAttendanceCalendarQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<EmployeeCalendarDto>>> HandleAsync(GetDailyAttendanceCalendarQuery query, CancellationToken cancellationToken = default)
    {
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

        var employees = await empQuery.OrderBy(e => e.FullName).ToListAsync(cancellationToken);
        if (employees.Count == 0)
        {
            return Result<IReadOnlyList<EmployeeCalendarDto>>.Success([]);
        }

        var empIds = employees.Select(e => e.Id).ToList();
        var attendances = await _dbContext.DailyAttendances
            .Where(a => empIds.Contains(a.EmployeeId) && a.Date >= query.StartDate && a.Date <= query.EndDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var attendancesByEmp = attendances
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(a => a.Date));

        var result = new List<EmployeeCalendarDto>();

        foreach (var emp in employees)
        {
            attendancesByEmp.TryGetValue(emp.Id, out var empAttMap);
            var daysList = new List<DailyAttendanceStatusDto>();

            for (var d = query.StartDate; d <= query.EndDate; d = d.AddDays(1))
            {
                if (empAttMap != null && empAttMap.TryGetValue(d, out var att))
                {
                    daysList.Add(new DailyAttendanceStatusDto(d, d.DayOfWeek.ToString(), att.Status.ToString(), att.LeaveType));
                }
                else
                {
                    var isWeekend = d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                    daysList.Add(new DailyAttendanceStatusDto(d, d.DayOfWeek.ToString(), isWeekend ? "WO" : "-", null));
                }
            }

            result.Add(new EmployeeCalendarDto(
                emp.Id,
                emp.EmployeeCode,
                emp.FullName,
                emp.Email,
                emp.Designation,
                emp.IsDeployed,
                daysList));
        }

        return Result<IReadOnlyList<EmployeeCalendarDto>>.Success(result);
    }
}
