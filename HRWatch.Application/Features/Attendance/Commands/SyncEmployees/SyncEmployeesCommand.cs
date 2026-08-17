using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Entities;
using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Attendance.Commands.SyncEmployees;

public record SyncEmployeesCommand(string TriggeredBy = "System") : ICommand<Result<SyncEmployeesResult>>;

public record SyncEmployeesResult(int TotalFetched, int EmployeesCreated, int EmployeesUpdated, DateTime SyncedAt);

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
            _logger.LogWarning("CG1 Master API returned 0 employees");
            return Result<SyncEmployeesResult>.Success(new SyncEmployeesResult(0, 0, 0, DateTime.UtcNow));
        }

        int created = 0;
        int updated = 0;

        foreach (var item in cg1Employees)
        {
            if (string.IsNullOrWhiteSpace(item.Email)) continue;

            var email = item.Email.Trim().ToLowerInvariant();
            var existing = await _dbContext.Employees
                .FirstOrDefaultAsync(e => e.Email.ToLower() == email, cancellationToken);

            var employeeCode = !string.IsNullOrWhiteSpace(item.EmployeeCode)
                ? item.EmployeeCode.Trim()
                : (item.Id > 0 ? $"CGI{item.Id}" : email);

            if (existing == null)
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
                created++;
            }
            else
            {
                existing.FullName = item.Name?.Trim() ?? existing.FullName;
                existing.Designation = item.Designation?.Trim() ?? existing.Designation;
                existing.IsDeployed = item.IsDeployed;
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(item.EmployeeCode))
                {
                    existing.EmployeeCode = item.EmployeeCode.Trim();
                }

                _dbContext.Employees.Update(existing);
                updated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee Master Sync completed. Total: {Total}, Created: {Created}, Updated: {Updated}",
            cg1Employees.Count, created, updated);

        return Result<SyncEmployeesResult>.Success(new SyncEmployeesResult(cg1Employees.Count, created, updated, DateTime.UtcNow));
    }
}
