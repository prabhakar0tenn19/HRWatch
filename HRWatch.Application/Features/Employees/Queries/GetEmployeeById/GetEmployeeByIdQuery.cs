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
        var dto = await _dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == query.Id)
            .Select(e => new EmployeeDetailDto(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Email,
                e.Designation,
                e.IsDeployed,
                e.IsActive,
                e.Location,
                e.CreatedAt,
                e.Exceptions.Count(),
                e.Attendances.Count()))
            .FirstOrDefaultAsync(cancellationToken);

        if (dto == null)
        {
            return Result<EmployeeDetailDto>.Failure("Employee not found.", "NOT_FOUND");
        }

        return Result<EmployeeDetailDto>.Success(dto);
    }
}
