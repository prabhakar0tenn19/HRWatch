using HRWatch.Application.Common.Abstractions;
using HRWatch.Domain.Entities;
using Microsoft.Extensions.Logging;
using DomainAttendance = HRWatch.Domain.Entities.Attendance;

namespace HRWatch.Application.Features.Attendance.Commands.SyncAttendance;


public class SyncAttendanceCommandHandler : ICommandHandler<SyncAttendanceCommand, SyncAttendanceResult>
{
    private readonly IAttendanceRepository  _attendanceRepository;
    private readonly IEmployeeRepository    _employeeRepository;
    private readonly IAttendanceApiClient   _attendanceApiClient;
    private readonly ILogger<SyncAttendanceCommandHandler> _logger;

    public SyncAttendanceCommandHandler(
        IAttendanceRepository  attendanceRepository,
        IEmployeeRepository    employeeRepository,
        IAttendanceApiClient   attendanceApiClient,
        ILogger<SyncAttendanceCommandHandler> logger)
    {
        _attendanceRepository = attendanceRepository;
        _employeeRepository   = employeeRepository;
        _attendanceApiClient  = attendanceApiClient;
        _logger               = logger;
    }

    public async Task<Result<SyncAttendanceResult>> HandleAsync(
        SyncAttendanceCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting attendance sync for {From} to {To}",
            command.FromDate.ToShortDateString(),
            command.ToDate.ToShortDateString());

        var externalRecords = await _attendanceApiClient.GetAttendanceAsync(
            command.FromDate, command.ToDate, command.EmployeeId, cancellationToken);

        if (externalRecords is null || externalRecords.Count == 0)
        {
            _logger.LogWarning("Attendance API returned 0 records for the given range.");
            return Result<SyncAttendanceResult>.Success(new SyncAttendanceResult(0, 0, 0, DateTime.UtcNow));
        }

        int created = 0, updated = 0;

        foreach (var record in externalRecords)
        {
            // Resolve employee by external ID
            var employee = await _employeeRepository.GetByExternalIdAsync(
                record.EmployeeExternalId, cancellationToken);

            if (employee is null)
            {
                _logger.LogWarning(
                    "Attendance record skipped: unknown employee ExternalId {ExternalId}",
                    record.EmployeeExternalId);
                continue;
            }

            var existing = await _attendanceRepository.GetByEmployeeAndDateAsync(
                employee.Id, record.Date, cancellationToken);

            if (existing is null)
            {
                var attendance = DomainAttendance.Create(
                    employee.Id,
                    record.Date,
                    record.CheckIn,
                    record.CheckOut,
                    record.Status,
                    record.ExternalReferenceId,
                    command.TriggeredBy);

                await _attendanceRepository.AddAsync(attendance, cancellationToken);
                created++;
            }
            else
            {
                existing.UpdateStatus(record.Status, command.TriggeredBy);
                await _attendanceRepository.UpdateAsync(existing, cancellationToken);
                updated++;
            }
        }

        await _attendanceRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Attendance sync done. Created: {C}, Updated: {U}", created, updated);

        return Result<SyncAttendanceResult>.Success(
            new SyncAttendanceResult(created + updated, created, updated, DateTime.UtcNow));
    }
}
