using HRWatch.Application.Common.Abstractions;
using HRWatch.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Employees.Commands.CreateEmployee;

public class CreateEmployeeCommandHandler : ICommandHandler<CreateEmployeeCommand, Guid>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<CreateEmployeeCommandHandler> _logger;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        ILogger<CreateEmployeeCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new employee with ExternalId: {ExternalId}", command.ExternalId);

       
        var existingExternal = await _employeeRepository.GetByExternalIdAsync(command.ExternalId, cancellationToken);
        if (existingExternal is not null)
        {
            return Result<Guid>.Failure(
                new Error("DUPLICATE_EXTERNAL_ID", $"Employee with ExternalId '{command.ExternalId}' already exists."));
        }

       
        var existingEmail = await _employeeRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existingEmail is not null)
        {
            return Result<Guid>.Failure(
                new Error("DUPLICATE_EMAIL", $"Employee with email '{command.Email}' already exists."));
        }

        var employee = Employee.Create(
            command.ExternalId,
            command.FirstName,
            command.LastName,
            command.Email,
            command.Department,
            command.JoinDate,
            command.CreatedBy);

        if (command.DesignationId.HasValue)
        {
            employee.AssignDesignation(command.DesignationId.Value);
        }

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created employee with Id: {EmployeeId}", employee.Id);
        return Result<Guid>.Success(employee.Id);
    }
}
