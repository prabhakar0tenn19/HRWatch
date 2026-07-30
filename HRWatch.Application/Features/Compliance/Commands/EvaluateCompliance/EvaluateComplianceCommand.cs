using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Compliance.Commands.EvaluateCompliance;


public record EvaluateComplianceCommand : ICommand<EvaluateComplianceResult>
{
    public DateTime PeriodStart { get; init; } = DateTime.UtcNow.AddMonths(-1).Date;
    public DateTime PeriodEnd   { get; init; } = DateTime.UtcNow.Date;
    public string   TriggeredBy { get; init; } = "system";

       public Guid? EmployeeId { get; init; }
}

public record EvaluateComplianceResult(
    int EmployeesEvaluated,
    int TotalViolationsFound,
    decimal AverageComplianceScore,
    DateTime EvaluatedAt);
