using HRWatch.Application.Common.Abstractions;
using HRWatch.Domain.Entities;
using HRWatch.Domain.Services;
using HRWatch.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Reports.Commands.GenerateWeeklyReport;


/// Handler: Generates the weekly report.
/// 
/// FLOW:
///   1. Get all active employees
///   2. For each employee, get their attendance for the week
///   3. Use PolicyEngine to find violations
///   4. Use ComplianceEvaluator to compute compliance score
///   5. Create WeeklyReport + WeeklyReportEntry for each employee
///   6. Save to database
/// 
/// This handler ORCHESTRATES the domain services.
/// The actual business rules live in PolicyEngine and ComplianceEvaluator.

public class GenerateWeeklyReportCommandHandler : ICommandHandler<GenerateWeeklyReportCommand, Guid>
{
    private readonly IEmployeeRepository    _employeeRepository;
    private readonly IAttendanceRepository  _attendanceRepository;
    private readonly IPolicyRepository      _policyRepository;
    private readonly IWeeklyReportRepository _reportRepository;
    private readonly IViolationRepository   _violationRepository;
    private readonly PolicyEngine           _policyEngine;
    private readonly ComplianceEvaluator    _complianceEvaluator;
    private readonly RuleEvaluator          _ruleEvaluator;
    private readonly ILogger<GenerateWeeklyReportCommandHandler> _logger;

    public GenerateWeeklyReportCommandHandler(
        IEmployeeRepository    employeeRepository,
        IAttendanceRepository  attendanceRepository,
        IPolicyRepository      policyRepository,
        IWeeklyReportRepository reportRepository,
        IViolationRepository   violationRepository,
        PolicyEngine           policyEngine,
        ComplianceEvaluator    complianceEvaluator,
        RuleEvaluator          ruleEvaluator,
        ILogger<GenerateWeeklyReportCommandHandler> logger)
    {
        _employeeRepository  = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _policyRepository    = policyRepository;
        _reportRepository    = reportRepository;
        _violationRepository = violationRepository;
        _policyEngine        = policyEngine;
        _complianceEvaluator = complianceEvaluator;
        _ruleEvaluator       = ruleEvaluator;
        _logger              = logger;
    }

    public async Task<Result<Guid>> HandleAsync(
        GenerateWeeklyReportCommand command,
        CancellationToken cancellationToken = default)
    {
        var period = new DateRange(command.WeekStartDate, command.WeekStartDate.AddDays(6));

        _logger.LogInformation("Generating weekly report for {Period}", period);

        // Check if report already exists for this period
        var existingReport = await _reportRepository.GetByPeriodAsync(period.Start, cancellationToken);
        if (existingReport is not null)
        {
            _logger.LogWarning("Weekly report for {Period} already exists (ID: {Id}). Skipping.", period, existingReport.Id);
            return Result<Guid>.Success(existingReport.Id);
        }

        // Step 1: Get all active employees
        var employees   = await _employeeRepository.GetActiveEmployeesAsync(cancellationToken);
        var activePolicies = await _policyRepository.GetActivePoliciesAsync(cancellationToken);

        // Step 2: Create the report shell
        var report = WeeklyReport.Create(period, employees.Count, command.TriggeredBy);

        // Step 3: Process each employee
        foreach (var employee in employees)
        {
            // Get their attendance for the week
            var weekAttendance = await _attendanceRepository.GetByEmployeeAndPeriodAsync(
                employee.Id, period.Start, period.End, cancellationToken);

            // Evaluate policy violations using PolicyEngine domain service
            var violations = _policyEngine.EvaluateAttendance(
                employee, weekAttendance, activePolicies, period.Start, period.End);

            // Persist new violations
            foreach (var violation in violations)
                await _violationRepository.AddAsync(violation, cancellationToken);

            // Compute compliance score using ComplianceEvaluator domain service
            var allViolations = await _violationRepository.GetByEmployeeAsync(employee.Id, cancellationToken);
            var weekViolations = allViolations
                .Where(v => v.OccurredOn >= period.Start && v.OccurredOn <= period.End)
                .ToList();

            var complianceResult = _complianceEvaluator.Evaluate(employee, weekAttendance, weekViolations);
            var summary          = _ruleEvaluator.ComputeSummary(weekAttendance);

            // Create per-employee entry in the report
            var entry = WeeklyReportEntry.Create(
                report.Id,
                employee.Id,
                employee.FullName,
                employee.Department,
                summary.DaysPresent,
                summary.DaysAbsent,
                summary.DaysLate,
                summary.DaysOnLeave,
                summary.TotalHours,
                weekViolations.Count,
                complianceResult.Score);

            report.AddEntry(entry);
        }

        // Step 4: Finalize and persist
        report.FinalizeReport();
        await _reportRepository.AddAsync(report, cancellationToken);
        await _reportRepository.SaveChangesAsync(cancellationToken);
        await _violationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Weekly report generated. ID: {ReportId}, Employees: {Count}, Score: {Score}",
            report.Id, employees.Count, report.ComplianceScore);

        return Result<Guid>.Success(report.Id);
    }
}
