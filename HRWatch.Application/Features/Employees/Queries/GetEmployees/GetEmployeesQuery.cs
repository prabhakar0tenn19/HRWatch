using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
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
            .Select(e => new EmployeeDto(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Email,
                e.Designation,
                e.IsDeployed,
                e.IsActive,
                e.Location,
                e.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<EmployeeDto>>.Success(employees);
    }
}
