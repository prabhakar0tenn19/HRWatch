using FluentValidation;

namespace HRWatch.Application.Features.Employees.Commands.SyncEmployees;



public class SyncEmployeesCommandValidator : AbstractValidator<SyncEmployeesCommand>
{
    public SyncEmployeesCommandValidator()
    {
  
        RuleFor(x => x.TriggeredBy)
            .NotEmpty()
            .WithMessage("TriggeredBy must be provided. Use 'system' for automated jobs.");

              RuleFor(x => x.DepartmentFilter)
            .Must(d => d is null || !string.IsNullOrWhiteSpace(d))
            .WithMessage("DepartmentFilter must not be empty whitespace.");
    }
}
