using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Application.Features.Employees.Queries.GetEmployeeById;

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
    int TotalExceptionsCount,
    int TotalAttendancesCount);

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
            .Include(e => e.Exceptions)
            .Include(e => e.Attendances)
            .FirstOrDefaultAsync(e => e.Id == query.Id, cancellationToken);

        if (emp == null)
        {
            return Result<EmployeeDetailDto>.Failure("Employee not found");
        }

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
            emp.Exceptions.Count,
            emp.Attendances.Count);

        return Result<EmployeeDetailDto>.Success(dto);
    }
}
