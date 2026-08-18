using HRWatch.Application.Common;
using HRWatch.Application.Features.Attendance.Commands.EvaluateDailyAttendance;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Attendance.Commands.EvaluateDateRange;

public record EvaluateDateRangeCommand(
    DateOnly StartDate,
    DateOnly EndDate,
    string TriggeredBy = "ManualTrigger"
) : ICommand<Result<EvaluateDateRangeResult>>;

public record EvaluateDateRangeResult(
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalDaysEvaluated,
    List<EvaluateDailyAttendanceResult> DailyResults,
    DateTime CompletedAt);

public class EvaluateDateRangeCommandHandler : ICommandHandler<EvaluateDateRangeCommand, Result<EvaluateDateRangeResult>>
{
    private readonly ICommandMediator _commandMediator;
    private readonly ILogger<EvaluateDateRangeCommandHandler> _logger;

    public EvaluateDateRangeCommandHandler(ICommandMediator commandMediator, ILogger<EvaluateDateRangeCommandHandler> logger)
    {
        _commandMediator = commandMediator;
        _logger = logger;
    }

    public async Task<Result<EvaluateDateRangeResult>> HandleAsync(EvaluateDateRangeCommand command, CancellationToken cancellationToken = default)
    {
        if (command.StartDate > command.EndDate)
        {
            return Result<EvaluateDateRangeResult>.Failure("StartDate cannot be after EndDate.", "INVALID_DATE_RANGE");
        }

        _logger.LogInformation("Starting Date Range Attendance Evaluation from {Start} to {End} triggered by {User}",
            command.StartDate, command.EndDate, command.TriggeredBy);

        var dailyResults = new List<EvaluateDailyAttendanceResult>();
        int daysCount = 0;

        for (var current = command.StartDate; current <= command.EndDate; current = current.AddDays(1))
        {
            var result = await _commandMediator.SendAsync(
                new EvaluateDailyAttendanceCommand(current, $"{command.TriggeredBy}:Range"),
                cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                dailyResults.Add(result.Value);
            }
            daysCount++;
        }

        _logger.LogInformation("Finished Date Range Attendance Evaluation for {Days} days from {Start} to {End}",
            daysCount, command.StartDate, command.EndDate);

        return Result<EvaluateDateRangeResult>.Success(new EvaluateDateRangeResult(
            command.StartDate,
            command.EndDate,
            daysCount,
            dailyResults,
            DateTime.UtcNow));
    }
}
