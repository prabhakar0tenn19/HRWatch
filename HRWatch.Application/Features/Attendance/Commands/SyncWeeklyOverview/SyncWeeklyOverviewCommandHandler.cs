using System.Text.Json;
using HRWatch.Application.Common.Abstractions;
using HRWatch.Domain.Entities;
using HRWatch.Domain.Services;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Attendance.Commands.SyncWeeklyOverview;

public class SyncWeeklyOverviewCommandHandler : ICommandHandler<SyncWeeklyOverviewCommand, SyncWeeklyOverviewResult>
{
    private readonly IEmployeeWeeklyOverviewApiClient _apiClient;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IWeeklyAttendanceRepository _weeklyAttendanceRepository;
    private readonly IPolicyRepository _policyRepository;
    private readonly IViolationRepository _violationRepository;
    private readonly IViolationCalculationService _violationService;
    private readonly ILogger<SyncWeeklyOverviewCommandHandler> _logger;

    public SyncWeeklyOverviewCommandHandler(
        IEmployeeWeeklyOverviewApiClient apiClient,
        IEmployeeRepository employeeRepository,
        IWeeklyAttendanceRepository weeklyAttendanceRepository,
        IPolicyRepository policyRepository,
        IViolationRepository violationRepository,
        IViolationCalculationService violationService,
        ILogger<SyncWeeklyOverviewCommandHandler> logger)
    {
        _apiClient = apiClient;
        _employeeRepository = employeeRepository;
        _weeklyAttendanceRepository = weeklyAttendanceRepository;
        _policyRepository = policyRepository;
        _violationRepository = violationRepository;
        _violationService = violationService;
        _logger = logger;
    }

    public async Task<Result<SyncWeeklyOverviewResult>> HandleAsync(
        SyncWeeklyOverviewCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting SyncWeeklyOverview command. TriggeredBy: {TriggeredBy}", command.TriggeredBy);

        var overviewList = await _apiClient.GetWeeklyOverviewAsync(cancellationToken);
        if (overviewList is null || overviewList.Count == 0)
        {
            _logger.LogWarning("Weekly Overview API returned empty list.");
            return Result<SyncWeeklyOverviewResult>.Success(
                new SyncWeeklyOverviewResult(0, 0, 0, DateTime.UtcNow));
        }

        var activePolicies = await _policyRepository.GetActivePoliciesAsync(cancellationToken);
        var defaultPolicy = activePolicies.FirstOrDefault();

        int employeesSynced = 0;
        int weeklyRecordsSynced = 0;

        foreach (var item in overviewList)
        {
            if (string.IsNullOrWhiteSpace(item.Email)) continue;

            var existingEmployee = await _employeeRepository.GetByEmailAsync(item.Email, cancellationToken);
            Employee employee;

            if (existingEmployee is null)
            {
                employee = Employee.Create(
                    externalId: item.Id.ToString(),
                    firstName: item.Name,
                    lastName: string.Empty,
                    email: item.Email,
                    department: "General",
                    joinDate: DateTime.UtcNow,
                    createdBy: command.TriggeredBy);

                await _employeeRepository.AddAsync(employee, cancellationToken);
                employeesSynced++;
            }
            else
            {
                employee = existingEmployee;
                employee.UpdateFromExternalApi(
                    firstName: item.Name,
                    lastName: string.Empty,
                    department: employee.Department ?? "General",
                    updatedBy: command.TriggeredBy);

                await _employeeRepository.UpdateAsync(employee, cancellationToken);
            }

            int presentCount = item.Leave.Count(l => string.Equals(l, "P", StringComparison.OrdinalIgnoreCase));
            int leaveCount = item.Leave.Count(l => !string.Equals(l, "P", StringComparison.OrdinalIgnoreCase));
            string rawLeaveJson = JsonSerializer.Serialize(item.Leave);

            var existingWeekly = await _weeklyAttendanceRepository.GetByEmployeeAndWeekAsync(
                employee.Id, item.StartDate, cancellationToken);

            WeeklyAttendance weekly;

            if (existingWeekly is null)
            {
                weekly = WeeklyAttendance.Create(
                    employeeId: employee.Id,
                    weekStartDate: item.StartDate,
                    weekEndDate: item.EndDate,
                    presentCount: presentCount,
                    leaveCount: leaveCount,
                    rawLeaveJson: rawLeaveJson,
                    createdBy: command.TriggeredBy);

                await _weeklyAttendanceRepository.AddAsync(weekly, cancellationToken);
                weeklyRecordsSynced++;
            }
            else
            {
                weekly = existingWeekly;
                weekly.UpdateAttendance(
                    presentCount: presentCount,
                    leaveCount: leaveCount,
                    rawLeaveJson: rawLeaveJson,
                    updatedBy: command.TriggeredBy);

                await _weeklyAttendanceRepository.UpdateAsync(weekly, cancellationToken);
                weeklyRecordsSynced++;
            }

            if (defaultPolicy != null)
            {
                var violation = _violationService.CalculateWfoViolation(employee, weekly, defaultPolicy);
                if (violation != null)
                {
                    await _violationRepository.AddAsync(violation, cancellationToken);
                    _logger.LogInformation("Violation flagged for Employee {Email}: {Description}", employee.Email, violation.Description);
                }
            }
        }

        await _employeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "SyncWeeklyOverview completed. Total: {Total}, Employees Synced: {Employees}, Records: {Records}",
            overviewList.Count, employeesSynced, weeklyRecordsSynced);

        return Result<SyncWeeklyOverviewResult>.Success(
            new SyncWeeklyOverviewResult(overviewList.Count, employeesSynced, weeklyRecordsSynced, DateTime.UtcNow));
    }
}
