using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Attendance.Commands.SyncAttendance;
using HRWatch.Application.Features.Reports.Commands.GenerateWeeklyReport;
using HRWatch.Application.Features.Employees.Commands.SyncEmployees;
using Microsoft.Extensions.Logging;

namespace HRWatch.Infrastructure.BackgroundJobs;

/// <summary>
/// HANGFIRE JOB: AttendanceSyncJob
/// 
/// This job runs every night (configured in DependencyInjection.cs).
/// 
/// CRITICAL RULE: Jobs NEVER contain business logic.
/// Jobs are just "triggers" that send commands through the mediator.
/// 
/// WHY?
/// - If logic is in the job, you can't test it without Hangfire
/// - If logic is in a handler, you can test it with a simple unit test
/// - The handler doesn't know or care WHO called it (job, controller, test)
/// 
/// FLOW:
///   Hangfire Scheduler → AttendanceSyncJob → ICommandMediator → SyncAttendanceCommandHandler
/// </summary>
public class AttendanceSyncJob
{
    private readonly ICommandMediator _commandMediator;
    private readonly ILogger<AttendanceSyncJob> _logger;

    public AttendanceSyncJob(ICommandMediator commandMediator, ILogger<AttendanceSyncJob> logger)
    {
        _commandMediator = commandMediator;
        _logger          = logger;
    }

    /// <summary>
    /// Syncs attendance for yesterday (run daily at midnight).
    /// Hangfire will call this method by name.
    /// </summary>
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

/// <summary>
/// HANGFIRE JOB: GenerateWeeklyReportJob
/// 
/// Runs every Monday at 6 AM to generate the previous week's compliance report.
/// </summary>
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

/// <summary>
/// HANGFIRE JOB: EmployeeSyncJob
/// 
/// Runs daily to sync employees from the external HR system.
/// </summary>
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

/// <summary>
/// HANGFIRE JOB: NotificationJob
/// 
/// Placeholder — will send notifications/alerts for pending violations.
/// </summary>
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
