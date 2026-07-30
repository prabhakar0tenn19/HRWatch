using HRWatch.Application.Common.Abstractions;
using HRWatch.Domain.Services;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Compliance.Commands.EvaluateCompliance;


public class EvaluateComplianceCommandHandler
    : ICommandHandler<EvaluateComplianceCommand, EvaluateComplianceResult>
{
    private readonly IEmployeeRepository   _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IPolicyRepository     _policyRepository;
    private readonly IViolationRepository  _violationRepository;
    private readonly PolicyEngine          _policyEngine;
    private readonly ComplianceEvaluator   _complianceEvaluator;
    private readonly ILogger<EvaluateComplianceCommandHandler> _logger;

    public EvaluateComplianceCommandHandler(
        IEmployeeRepository   employeeRepository,
        IAttendanceRepository attendanceRepository,
        IPolicyRepository     policyRepository,
        IViolationRepository  violationRepository,
        PolicyEngine          policyEngine,
        ComplianceEvaluator   complianceEvaluator,
        ILogger<EvaluateComplianceCommandHandler> logger)
    {
        _employeeRepository   = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _policyRepository     = policyRepository;
        _violationRepository  = violationRepository;
        _policyEngine         = policyEngine;
        _complianceEvaluator  = complianceEvaluator;
        _logger               = logger;
    }

    public async Task<Result<EvaluateComplianceResult>> HandleAsync(
        EvaluateComplianceCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting compliance evaluation for {Start} to {End}",
            command.PeriodStart, command.PeriodEnd);

        var employees = command.EmployeeId.HasValue
            ? new[] { await _employeeRepository.GetByIdAsync(command.EmployeeId.Value, cancellationToken) }
                .Where(e => e is not null).Select(e => e!).ToList()
            : (await _employeeRepository.GetActiveEmployeesAsync(cancellationToken)).ToList();

        var activePolicies   = await _policyRepository.GetActivePoliciesAsync(cancellationToken);
        var totalViolations  = 0;
        var scoreSum         = 0m;

        foreach (var employee in employees)
        {
            var attendance = await _attendanceRepository.GetByEmployeeAndPeriodAsync(
                employee.Id, command.PeriodStart, command.PeriodEnd, cancellationToken);

            var violations = _policyEngine.EvaluateAttendance(
                employee, attendance, activePolicies, command.PeriodStart, command.PeriodEnd);

            foreach (var v in violations)
                await _violationRepository.AddAsync(v, cancellationToken);

            var allViolations  = violations.ToList();
            var compResult     = _complianceEvaluator.Evaluate(employee, attendance, allViolations);
            totalViolations   += violations.Count;
            scoreSum          += compResult.Score;
        }

        if (employees.Count > 0)
            await _violationRepository.SaveChangesAsync(cancellationToken);

        var avgScore = employees.Count > 0
            ? Math.Round(scoreSum / employees.Count, 2)
            : 100m;

        _logger.LogInformation(
            "Compliance evaluation done. Employees: {Count}, Violations: {V}, AvgScore: {Score}",
            employees.Count, totalViolations, avgScore);

        return Result<EvaluateComplianceResult>.Success(
            new EvaluateComplianceResult(
                employees.Count,
                totalViolations,
                avgScore,
                DateTime.UtcNow));
    }
}
