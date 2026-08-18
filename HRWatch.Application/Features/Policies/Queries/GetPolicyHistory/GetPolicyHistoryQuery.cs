using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Application.Features.Policies.Queries.GetActivePolicy;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Application.Features.Policies.Queries.GetPolicyHistory;

public record GetPolicyHistoryQuery : IQuery<Result<IReadOnlyList<PolicyDto>>>;

public class GetPolicyHistoryQueryHandler : IQueryHandler<GetPolicyHistoryQuery, Result<IReadOnlyList<PolicyDto>>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPolicyHistoryQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<PolicyDto>>> HandleAsync(GetPolicyHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var policies = await _dbContext.Policies
            .OrderByDescending(p => p.Version)
            .Select(p => new PolicyDto(
                p.Id,
                p.Version,
                p.PolicyName,
                p.RulesJson,
                p.EffectiveFrom,
                p.EffectiveTo,
                p.IsActive,
                p.CreatedBy,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PolicyDto>>.Success(policies);
    }
}
