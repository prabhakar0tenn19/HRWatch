using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Entities;
using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Policies.Commands.CreateNewPolicyVersion;

public record CreateNewPolicyVersionCommand(
    string PolicyName,
    string RulesJson,
    DateOnly EffectiveFrom,
    string CreatedBy = "Admin"
) : ICommand<Result<Guid>>;

public class CreateNewPolicyVersionCommandHandler : ICommandHandler<CreateNewPolicyVersionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CreateNewPolicyVersionCommandHandler> _logger;

    public CreateNewPolicyVersionCommandHandler(IApplicationDbContext dbContext, ILogger<CreateNewPolicyVersionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(CreateNewPolicyVersionCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RulesJson))
        {
            return Result<Guid>.Failure("RulesJson configuration cannot be empty.", "VALIDATION_ERROR");
        }

        var currentPolicy = await _dbContext.Policies
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(cancellationToken);

        int nextVersion = 1;
        if (currentPolicy != null)
        {
            currentPolicy.IsActive = false;
            currentPolicy.EffectiveTo = command.EffectiveFrom.AddDays(-1);
            currentPolicy.UpdatedAt = DateTime.UtcNow;
            _dbContext.Policies.Update(currentPolicy);

            nextVersion = currentPolicy.Version + 1;
        }

        var newPolicy = new Policy
        {
            Version = nextVersion,
            PolicyName = command.PolicyName,
            RulesJson = command.RulesJson,
            EffectiveFrom = command.EffectiveFrom,
            EffectiveTo = null,
            IsActive = true,
            CreatedBy = command.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Policies.AddAsync(newPolicy, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated Policy Version {Version} effective from {From} by {User}",
            newPolicy.Version, newPolicy.EffectiveFrom, newPolicy.CreatedBy);

        return Result<Guid>.Success(newPolicy.Id);
    }
}
