using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Entities;
using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Exceptions.Commands.CreateException;

public record CreateExceptionCommand(
    Guid EmployeeId,
    DateOnly FromDate,
    DateOnly ToDate,
    string Reason,
    string CreatedBy = "HR"
) : ICommand<Result<Guid>>;

public class CreateExceptionCommandHandler : ICommandHandler<CreateExceptionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CreateExceptionCommandHandler> _logger;

    public CreateExceptionCommandHandler(IApplicationDbContext dbContext, ILogger<CreateExceptionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(CreateExceptionCommand command, CancellationToken cancellationToken = default)
    {
        if (command.FromDate > command.ToDate)
        {
            return Result<Guid>.Failure("FromDate cannot be after ToDate.", "INVALID_DATE_RANGE");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result<Guid>.Failure("Reason is required.", "VALIDATION_ERROR");
        }

        var employeeExists = await _dbContext.Employees.AnyAsync(e => e.Id == command.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return Result<Guid>.Failure("Employee not found.", "NOT_FOUND");
        }

        // Check overlapping active exceptions for this employee
        bool hasOverlap = await _dbContext.EmployeeExceptions
            .AnyAsync(e => e.EmployeeId == command.EmployeeId &&
                           e.IsActive &&
                           e.FromDate <= command.ToDate &&
                           e.ToDate >= command.FromDate,
                      cancellationToken);

        if (hasOverlap)
        {
            _logger.LogWarning("Rejected overlapping exception for Employee {EmpId} between {From} and {To}",
                command.EmployeeId, command.FromDate, command.ToDate);
            return Result<Guid>.Failure("An active exception already exists for this employee within the selected date range.", "OVERLAPPING_EXCEPTION");
        }

        var exception = new EmployeeException
        {
            EmployeeId = command.EmployeeId,
            FromDate = command.FromDate,
            ToDate = command.ToDate,
            Reason = command.Reason.Trim(),
            CreatedBy = command.CreatedBy,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.EmployeeExceptions.AddAsync(exception, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Exception created for Employee {EmpId} from {From} to {To} by {User}",
            command.EmployeeId, command.FromDate, command.ToDate, command.CreatedBy);

        return Result<Guid>.Success(exception.Id);
    }
}
