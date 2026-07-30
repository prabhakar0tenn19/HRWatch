using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Attendance.Commands.SyncAttendance;
using HRWatch.Application.Features.Reports.Commands.GenerateWeeklyReport;
using HRWatch.Application.Features.Employees.Commands.SyncEmployees;
using Microsoft.Extensions.Logging;

namespace HRWatch.Infrastructure.BackgroundJobs;


public class AttendanceSyncJob
{
    private readonly ICommandMediator _commandMediator;
    private readonly ILogger<AttendanceSyncJob> _logger;

    public AttendanceSyncJob(ICommandMediator commandMediator, ILogger<AttendanceSyncJob> logger)
    {
        _commandMediator = commandMediator;
        _logger          = logger;
    }

   
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[AttendanceSyncJob] Starting...");

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);

        // The job just sends a command. Business logic is in the handler.
        var result = await _commandMediator.SendAsync(new SyncAttendanceCommand
        {
            FromDate    = yesterday,
            ToDate      = yesterday,
            TriggeredBy = "AttendanceSyncJob"
        });

        if (result.IsSuccess)
            _logger.LogInformation("[AttendanceSyncJob] Done. Synced {Count} records.",
                result.Value?.RecordsSynced ?? 0);
        else
            _logger.LogError("[AttendanceSyncJob] Failed: {Error}", result.Error);
    }
}


public class GenerateWeeklyReportJob
{
    private readonly ICommandMediator _commandMediator;
    private readonly ILogger<GenerateWeeklyReportJob> _logger;

    public GenerateWeeklyReportJob(ICommandMediator commandMediator, ILogger<GenerateWeeklyReportJob> logger)
    {
        _commandMediator = commandMediator;
        _logger          = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[GenerateWeeklyReportJob] Starting...");

        var result = await _commandMediator.SendAsync(new GenerateWeeklyReportCommand
        {
            TriggeredBy = "GenerateWeeklyReportJob"
        });

        if (result.IsSuccess)
            _logger.LogInformation("[GenerateWeeklyReportJob] Report generated. ID: {Id}", result.Value);
        else
            _logger.LogError("[GenerateWeeklyReportJob] Failed: {Error}", result.Error);
    }
}


public class EmployeeSyncJob
{
    private readonly ICommandMediator _commandMediator;
    private readonly ILogger<EmployeeSyncJob> _logger;

    public EmployeeSyncJob(ICommandMediator commandMediator, ILogger<EmployeeSyncJob> logger)
    {
        _commandMediator = commandMediator;
        _logger          = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[EmployeeSyncJob] Starting...");

        var result = await _commandMediator.SendAsync(new SyncEmployeesCommand
        {
            TriggeredBy = "EmployeeSyncJob"
        });

        if (result.IsSuccess)
            _logger.LogInformation("[EmployeeSyncJob] Synced {Count} employees.",
                result.Value?.EmployeesSynced ?? 0);
        else
            _logger.LogError("[EmployeeSyncJob] Failed: {Error}", result.Error);
    }
}

public class NotificationJob
{
    private readonly ILogger<NotificationJob> _logger;

    public NotificationJob(ILogger<NotificationJob> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync()
    {
        _logger.LogInformation("[NotificationJob] Placeholder — notifications not yet implemented.");
        return Task.CompletedTask;
    }
}
