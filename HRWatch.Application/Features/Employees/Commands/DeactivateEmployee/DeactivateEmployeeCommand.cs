using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Employees.Commands.DeactivateEmployee;

public record DeactivateEmployeeCommand(
    Guid Id,
    DateTime? TerminationDate = null,
    string DeactivatedBy = "system"
) : ICommand<Unit>;
