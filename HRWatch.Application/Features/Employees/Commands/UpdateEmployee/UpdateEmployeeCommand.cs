using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Employees.Commands.UpdateEmployee;

public record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Department,
    string? PhoneNumber) : ICommand<Unit>;
