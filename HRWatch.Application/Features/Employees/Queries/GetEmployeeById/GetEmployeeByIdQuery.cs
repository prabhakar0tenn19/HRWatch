using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Enums;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Application.Features.Employees.Queries.GetEmployeeById;

public record RecentAttendanceDto(
    DateOnly Date,
    string DayOfWeek,
    string Status,
    string? LeaveType,
    string? FirstPunchTime);

public record EmployeeDetailDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string Designation,
    bool IsDeployed,
    bool IsActive,
    string Location,
    DateTime CreatedAt,
    int PresentDays,
    int AbsentDays,
    int LeaveDays,
    int WfhDays,
    int ExceptionDays,
    double AbsentPercentage,
    int TotalExceptionsCount,
    IReadOnlyList<RecentAttendanceDto> RecentAttendances);

public record GetEmployeeByIdQuery(Guid Id) : IQuery<Result<EmployeeDetailDto>>;

public class GetEmployeeByIdQueryHandler : IQueryHandler<GetEmployeeByIdQuery, Result<EmployeeDetailDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetEmployeeByIdQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<EmployeeDetailDto>> HandleAsync(GetEmployeeByIdQuery query, CancellationToken cancellationToken = default)
    {
        var emp = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == query.Id)
            .Select(e => new
            {
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Email,
                e.Designation,
                e.IsDeployed,
                e.IsActive,
                e.Location,
                e.CreatedAt,
                PresentDays = e.Attendances.Count(a => a.Status == AttendanceStatus.P),
                AbsentDays = e.Attendances.Count(a => a.Status == AttendanceStatus.A),
                LeaveDays = e.Attendances.Count(a => a.Status == AttendanceStatus.L),
                WfhDays = e.Attendances.Count(a => a.Status == AttendanceStatus.W),
                ExceptionDays = e.Attendances.Count(a => a.Status == AttendanceStatus.E),
                TotalWorkingEvaluated = e.Attendances.Count(a => a.Status != AttendanceStatus.WO && a.Status != AttendanceStatus.H),
                TotalExceptionsCount = e.Exceptions.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (emp == null)
        {
            return Result<EmployeeDetailDto>.Failure("Employee not found.", "NOT_FOUND");
        }

        // Fetch last 10 evaluated attendances for the drawer feed
        var recentAtts = await _dbContext.DailyAttendances
            .Where(a => a.EmployeeId == query.Id)
            .OrderByDescending(a => a.Date)
            .Take(10)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var recentDates = recentAtts.Select(a => a.Date).ToList();

        // Fetch punch logs for these dates to show first punch time
        var punchLogs = await _dbContext.DailyPunchLogs
            .Where(p => p.EmployeeId == query.Id && recentDates.Contains(p.PunchDate))
            .OrderBy(p => p.PunchTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var firstPunchesByDate = punchLogs
            .GroupBy(p => p.PunchDate)
            .ToDictionary(g => g.Key, g => g.First().PunchTime.ToString("hh:mm tt"));

        var recentDtos = recentAtts.Select(a =>
        {
            firstPunchesByDate.TryGetValue(a.Date, out var punchTime);
            return new RecentAttendanceDto(
                a.Date,
                a.Date.DayOfWeek.ToString(),
                a.Status.ToString(),
                a.LeaveType,
                punchTime);
        }).ToList();

        double absentPct = emp.TotalWorkingEvaluated > 0
            ? Math.Round((double)emp.AbsentDays / emp.TotalWorkingEvaluated * 100.0, 1)
            : 0.0;

        var dto = new EmployeeDetailDto(
            emp.Id,
            emp.EmployeeCode,
            emp.FullName,
            emp.Email,
            emp.Designation,
            emp.IsDeployed,
            emp.IsActive,
            emp.Location,
            emp.CreatedAt,
            emp.PresentDays,
            emp.AbsentDays,
            emp.LeaveDays,
            emp.WfhDays,
            emp.ExceptionDays,
            absentPct,
            emp.TotalExceptionsCount,
            recentDtos);

        return Result<EmployeeDetailDto>.Success(dto);
    }
}
