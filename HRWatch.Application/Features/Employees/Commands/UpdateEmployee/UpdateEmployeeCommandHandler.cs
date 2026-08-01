using HRWatch.Application.Common.Abstractions;
using HRWatch.Domain.Common;

namespace HRWatch.Application.Features.Employees.Commands.UpdateEmployee;

public class UpdateEmployeeCommandHandler : ICommandHandler<UpdateEmployeeCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;

    public UpdateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<Unit>> HandleAsync(
        UpdateEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(command.Id, cancellationToken);
        if (employee is null)
        {
            return Result<Unit>.Failure(
                new Error("Employee.NotFound", $"Employee with ID '{command.Id}' was not found."));
        }

        employee.UpdateDetails(
            command.FirstName,
            command.LastName,
            command.Department,
            command.PhoneNumber,
            "system");

        await _employeeRepository.UpdateAsync(employee, cancellationToken);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
