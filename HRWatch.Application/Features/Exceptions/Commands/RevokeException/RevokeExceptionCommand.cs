using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRWatch.Application.Features.Exceptions.Commands.RevokeException;

public record RevokeExceptionCommand(Guid ExceptionId) : ICommand<Result>;

public class RevokeExceptionCommandHandler : ICommandHandler<RevokeExceptionCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;

    public RevokeExceptionCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> HandleAsync(RevokeExceptionCommand command, CancellationToken cancellationToken = default)
    {
        var ex = await _dbContext.EmployeeExceptions.FirstOrDefaultAsync(e => e.Id == command.ExceptionId, cancellationToken);
        if (ex == null)
        {
            return Result.Failure("Exception record not found.", "NOT_FOUND");
        }

        ex.IsActive = false;
        ex.UpdatedAt = DateTime.UtcNow;

        _dbContext.EmployeeExceptions.Update(ex);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
