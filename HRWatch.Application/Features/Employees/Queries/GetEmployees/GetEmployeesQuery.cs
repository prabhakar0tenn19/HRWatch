using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Enums;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Application.Features.Employees.Queries.GetEmployees;

public record EmployeeDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string Designation,
    bool IsDeployed,
    bool IsActive,
    string Location,
    int PresentDays,
    int AbsentDays,
    int LeaveDays,
    int WfhDays,
    int ExceptionDays,
    double AbsentPercentage,
    DateTime CreatedAt);

public record GetEmployeesQuery(
    string? SearchTerm = null,
    string? Designation = null,
    bool? IsDeployed = null,
    bool OnlyActive = true) : IQuery<Result<IReadOnlyList<EmployeeDto>>>;

public class GetEmployeesQueryHandler : IQueryHandler<GetEmployeesQuery, Result<IReadOnlyList<EmployeeDto>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetEmployeesQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<EmployeeDto>>> HandleAsync(GetEmployeesQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _dbContext.Employees.AsNoTracking();

        if (query.OnlyActive)
        {
            dbQuery = dbQuery.Where(e => e.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(e =>
                e.FullName.ToLower().Contains(term) ||
                e.Email.ToLower().Contains(term) ||
                e.EmployeeCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Designation))
        {
            var desig = query.Designation.Trim().ToLower();
            dbQuery = dbQuery.Where(e => e.Designation.ToLower().Contains(desig));
        }

        if (query.IsDeployed.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.IsDeployed == query.IsDeployed.Value);
        }

        var employees = await dbQuery
            .OrderBy(e => e.FullName)
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
                TotalWorkingEvaluated = e.Attendances.Count(a => a.Status != AttendanceStatus.WO && a.Status != AttendanceStatus.H)
            })
            .ToListAsync(cancellationToken);

        var result = employees.Select(e =>
        {
            double absentPct = e.TotalWorkingEvaluated > 0
                ? Math.Round((double)e.AbsentDays / e.TotalWorkingEvaluated * 100.0, 1)
                : 0.0;

            return new EmployeeDto(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Email,
                e.Designation,
                e.IsDeployed,
                e.IsActive,
                e.Location,
                e.PresentDays,
                e.AbsentDays,
                e.LeaveDays,
                e.WfhDays,
                e.ExceptionDays,
                absentPct,
                e.CreatedAt);
        }).ToList();

        return Result<IReadOnlyList<EmployeeDto>>.Success(result);
    }
}
