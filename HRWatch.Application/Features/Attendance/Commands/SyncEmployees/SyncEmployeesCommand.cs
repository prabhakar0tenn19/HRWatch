using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Entities;
using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Attendance.Commands.SyncEmployees;

public record SyncEmployeesCommand(string TriggeredBy = "System") : ICommand<Result<SyncEmployeesResult>>;

public record SyncEmployeesResult(
    int TotalFetched,
    int EmployeesCreated,
    int EmployeesUpdated,
    int EmployeesDeactivated,
    DateTime SyncedAt);

public class SyncEmployeesCommandHandler : ICommandHandler<SyncEmployeesCommand, Result<SyncEmployeesResult>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICg1ApiClient _cg1ApiClient;
    private readonly ILogger<SyncEmployeesCommandHandler> _logger;

    public SyncEmployeesCommandHandler(
        IApplicationDbContext dbContext,
        ICg1ApiClient cg1ApiClient,
        ILogger<SyncEmployeesCommandHandler> logger)
    {
        _dbContext = dbContext;
        _cg1ApiClient = cg1ApiClient;
        _logger = logger;
    }

    public async Task<Result<SyncEmployeesResult>> HandleAsync(SyncEmployeesCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Employee Master Sync triggered by {TriggeredBy}", command.TriggeredBy);

        var cg1Employees = await _cg1ApiClient.GetMasterEmployeesAsync(cancellationToken);
        if (cg1Employees.Count == 0)
        {
            _logger.LogWarning("CG1 Master API returned 0 employees. Skipping deactivation safety check to protect existing data.");
            return Result<SyncEmployeesResult>.Success(new SyncEmployeesResult(0, 0, 0, 0, DateTime.UtcNow));
        }

        // 1. Collect all incoming active emails
        var incomingActiveEmails = cg1Employees
            .Where(e => !string.IsNullOrWhiteSpace(e.Email))
            .Select(e => e.Email.Trim().ToLowerInvariant())
            .ToHashSet();

        // 2. Fetch all existing local India employees
        var allLocalEmployees = await _dbContext.Employees
            .Where(e => e.Location == "India")
            .ToListAsync(cancellationToken);

        var localEmpMap = allLocalEmployees.ToDictionary(
            e => e.Email.Trim().ToLowerInvariant(),
            StringComparer.OrdinalIgnoreCase);

        int created = 0;
        int updated = 0;
        int deactivated = 0;

        // 3. Upsert Active Employees from CG1
        foreach (var item in cg1Employees)
        {
            if (string.IsNullOrWhiteSpace(item.Email)) continue;

            var email = item.Email.Trim().ToLowerInvariant();
            var employeeCode = !string.IsNullOrWhiteSpace(item.EmployeeCode)
                ? item.EmployeeCode.Trim()
                : (item.Id > 0 ? $"CGI{item.Id}" : email);

            if (!localEmpMap.TryGetValue(email, out var existing))
            {
                var newEmp = new Employee
                {
                    EmployeeCode = employeeCode,
                    FullName = item.Name?.Trim() ?? string.Empty,
                    Email = item.Email.Trim(),
                    Designation = item.Designation?.Trim() ?? "SDE",
                    IsDeployed = item.IsDeployed,
                    IsActive = true,
                    Location = "India",
                    CreatedAt = DateTime.UtcNow
                };

                await _dbContext.Employees.AddAsync(newEmp, cancellationToken);
                localEmpMap[email] = newEmp;
                created++;
            }
            else
            {
                existing.FullName = item.Name?.Trim() ?? existing.FullName;
                existing.Designation = item.Designation?.Trim() ?? existing.Designation;
                existing.IsDeployed = item.IsDeployed;
                existing.IsActive = true; // Ensure active
                existing.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(item.EmployeeCode))
                {
                    existing.EmployeeCode = item.EmployeeCode.Trim();
                }

                _dbContext.Employees.Update(existing);
                updated++;
            }
        }

        // 4. Auto-Deactivate Employees not present in incoming active list
        foreach (var localEmp in allLocalEmployees)
        {
            var email = localEmp.Email.Trim().ToLowerInvariant();
            if (localEmp.IsActive && !incomingActiveEmails.Contains(email))
            {
                localEmp.IsActive = false; // Deactivated (Soft delete)
                localEmp.UpdatedAt = DateTime.UtcNow;
                _dbContext.Employees.Update(localEmp);
                deactivated++;
                _logger.LogInformation("Deactivated employee no longer in CG1 active list: {Email} ({Code})",
                    localEmp.Email, localEmp.EmployeeCode);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee Master Sync completed. Total: {Total}, Created: {Created}, Updated: {Updated}, Deactivated: {Deactivated}",
            cg1Employees.Count, created, updated, deactivated);

        return Result<SyncEmployeesResult>.Success(
            new SyncEmployeesResult(cg1Employees.Count, created, updated, deactivated, DateTime.UtcNow));
    }
}
