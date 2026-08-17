using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Application.Features.Policies.Queries.GetActivePolicy;

public record GetActivePolicyQuery : IQuery<Result<PolicyDto>>;

public record PolicyDto(
    Guid Id,
    int Version,
    string PolicyName,
    string RulesJson,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt);

public class GetActivePolicyQueryHandler : IQueryHandler<GetActivePolicyQuery, Result<PolicyDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetActivePolicyQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PolicyDto>> HandleAsync(GetActivePolicyQuery query, CancellationToken cancellationToken = default)
    {
        var policy = await _dbContext.Policies
            .Where(p => p.IsActive)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (policy == null)
        {
            return Result<PolicyDto>.Failure("No active policy found.", "NOT_FOUND");
        }

        return Result<PolicyDto>.Success(policy);
    }
}
