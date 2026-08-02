using HRWatch.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Employees.Commands.DeactivateEmployee;

public class DeactivateEmployeeCommandHandler : ICommandHandler<DeactivateEmployeeCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<DeactivateEmployeeCommandHandler> _logger;

    public DeactivateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        ILogger<DeactivateEmployeeCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    public async Task<Result<Unit>> HandleAsync(
        DeactivateEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(command.Id, cancellationToken);
        if (employee is null)
        {
            return Result<Unit>.Failure("Employee.NotFound", $"Employee with ID '{command.Id}' was not found.");
        }

        var termDate = command.TerminationDate ?? DateTime.UtcNow;
        employee.Deactivate(termDate, command.DeactivatedBy);

        await _employeeRepository.UpdateAsync(employee, cancellationToken);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee {EmployeeId} deactivated by {DeactivatedBy}", command.Id, command.DeactivatedBy);

        return Result<Unit>.Success(Unit.Value);
    }
}
