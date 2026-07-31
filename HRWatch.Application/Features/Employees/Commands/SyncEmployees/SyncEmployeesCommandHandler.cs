using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Employees.DTOs;
using HRWatch.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Employees.Commands.SyncEmployees;

public class SyncEmployeesCommandHandler : ICommandHandler<SyncEmployeesCommand, SyncEmployeesResult>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEmployeeApiClient _employeeApiClient;
    private readonly ILogger<SyncEmployeesCommandHandler> _logger;

    public SyncEmployeesCommandHandler(
        IEmployeeRepository employeeRepository,
        IEmployeeApiClient employeeApiClient,
        ILogger<SyncEmployeesCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _employeeApiClient = employeeApiClient;
        _logger = logger;
    }

    public async Task<Result<SyncEmployeesResult>> HandleAsync(
        SyncEmployeesCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting employee sync. TriggeredBy: {TriggeredBy}", command.TriggeredBy);

        var externalEmployees = await _employeeApiClient.GetAllEmployeesAsync(cancellationToken);

        if (externalEmployees is null || externalEmployees.Count == 0)
        {
            _logger.LogWarning("External Employee API returned 0 employees.");
            return Result<SyncEmployeesResult>.Success(
                new SyncEmployeesResult(0, 0, 0, DateTime.UtcNow));
        }

        if (!string.IsNullOrWhiteSpace(command.DepartmentFilter))
        {
            externalEmployees = externalEmployees
                .Where(e => string.Equals(e.Department, command.DepartmentFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        int created = 0;
        int updated = 0;

        foreach (var externalEmployee in externalEmployees)
        {
            var existing = await _employeeRepository.GetByExternalIdAsync(
                externalEmployee.ExternalId, cancellationToken);

            if (existing is null)
            {
                var employee = Employee.Create(
                    externalEmployee.ExternalId,
                    externalEmployee.FirstName,
                    externalEmployee.LastName,
                    externalEmployee.Email,
                    externalEmployee.Department,
                    externalEmployee.JoinDate,
                    command.TriggeredBy);

                await _employeeRepository.AddAsync(employee, cancellationToken);
                created++;
            }
            else
            {
                existing.UpdateFromExternalApi(
                    externalEmployee.FirstName,
                    externalEmployee.LastName,
                    externalEmployee.Department,
                    command.TriggeredBy);

                await _employeeRepository.UpdateAsync(existing, cancellationToken);
                updated++;
            }
        }

        await _employeeRepository.SaveChangesAsync(cancellationToken);

        var result = new SyncEmployeesResult(
            EmployeesSynced: created + updated,
            EmployeesCreated: created,
            EmployeesUpdated: updated,
            SyncedAt: DateTime.UtcNow);

        _logger.LogInformation(
            "Employee sync complete. Created: {Created}, Updated: {Updated}",
            created, updated);

        return Result<SyncEmployeesResult>.Success(result);
    }
}
