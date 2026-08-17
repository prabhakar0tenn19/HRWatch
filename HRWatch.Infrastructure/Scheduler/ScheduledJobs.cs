using Coravel.Invocable;
using HRWatch.Application.Features.Attendance.Commands.EvaluateDailyAttendance;
using HRWatch.Application.Features.Attendance.Commands.SyncEmployees;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace HRWatch.Infrastructure.Scheduler;

public class DailyAttendanceEvaluationJob : IInvocable
{
    private readonly ICommandMediator _commandMediator;
    private readonly ILogger<DailyAttendanceEvaluationJob> _logger;

    public DailyAttendanceEvaluationJob(ICommandMediator commandMediator, ILogger<DailyAttendanceEvaluationJob> logger)
    {
        _commandMediator = commandMediator;
        _logger = logger;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("[Coravel Scheduler] Starting scheduled Daily Attendance Evaluation...");
        var result = await _commandMediator.SendAsync(new EvaluateDailyAttendanceCommand(null, "CoravelScheduler"));
        if (result.IsSuccess)
        {
            _logger.LogInformation("[Coravel Scheduler] Daily Attendance Evaluation finished successfully. Present: {P}, Absent: {A}",
                result.Value?.PresentCount, result.Value?.AbsentCount);
        }
        else
        {
            _logger.LogError("[Coravel Scheduler] Daily Attendance Evaluation failed: {Error}", result.ErrorMessage);
        }
    }
}

public class DailyEmployeeSyncJob : IInvocable
{
    private readonly ICommandMediator _commandMediator;
    private readonly ILogger<DailyEmployeeSyncJob> _logger;

    public DailyEmployeeSyncJob(ICommandMediator commandMediator, ILogger<DailyEmployeeSyncJob> logger)
    {
        _commandMediator = commandMediator;
        _logger = logger;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("[Coravel Scheduler] Starting scheduled Employee Master Sync...");
        var result = await _commandMediator.SendAsync(new SyncEmployeesCommand("CoravelScheduler"));
        if (result.IsSuccess)
        {
            _logger.LogInformation("[Coravel Scheduler] Employee Master Sync finished. Total: {Total}, Created: {Created}",
                result.Value?.TotalFetched, result.Value?.EmployeesCreated);
        }
        else
        {
            _logger.LogError("[Coravel Scheduler] Employee Master Sync failed: {Error}", result.ErrorMessage);
        }
    }
}
