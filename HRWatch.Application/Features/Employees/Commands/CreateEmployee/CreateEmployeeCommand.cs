using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Employees.Commands.CreateEmployee{

public record CreateEmployeeCommand(
    string ExternalId,
    string FirstName,
    string LastName,
    string Email,
    string Department,
    DateTime JoinDate,
    Guid? DesignationId = null,
    string CreatedBy = "system") : ICommand<Guid>;
}