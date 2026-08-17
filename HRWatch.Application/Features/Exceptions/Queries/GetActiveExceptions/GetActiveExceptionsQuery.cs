using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Application.Features.Exceptions.Queries.GetActiveExceptions;

public record GetActiveExceptionsQuery(
    Guid? EmployeeId = null,
    bool ActiveOnly = true
) : IQuery<Result<IReadOnlyList<EmployeeExceptionDto>>>;

public record EmployeeExceptionDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    string Email,
    DateOnly FromDate,
    DateOnly ToDate,
    string Reason,
    string CreatedBy,
    bool IsActive,
    DateTime CreatedAt);

public class GetActiveExceptionsQueryHandler : IQueryHandler<GetActiveExceptionsQuery, Result<IReadOnlyList<EmployeeExceptionDto>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetActiveExceptionsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<EmployeeExceptionDto>>> HandleAsync(GetActiveExceptionsQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _dbContext.EmployeeExceptions
            .Include(e => e.Employee)
            .AsNoTracking();

        if (query.ActiveOnly)
        {
            dbQuery = dbQuery.Where(e => e.IsActive);
        }

        if (query.EmployeeId.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.EmployeeId == query.EmployeeId.Value);
        }

        var list = await dbQuery
            .OrderByDescending(e => e.FromDate)
            .Select(e => new EmployeeExceptionDto(
                e.Id,
                e.EmployeeId,
                e.Employee != null ? e.Employee.EmployeeCode : string.Empty,
                e.Employee != null ? e.Employee.FullName : "Unknown",
                e.Employee != null ? e.Employee.Email : string.Empty,
                e.FromDate,
                e.ToDate,
                e.Reason,
                e.CreatedBy,
                e.IsActive,
                e.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<EmployeeExceptionDto>>.Success(list);
    }
}
