using HRWatch.Application.Common.Abstractions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Common.Behaviors;

public class ValidationBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _next;
    private readonly IValidator<TCommand>? _validator;
    private readonly ILogger<ValidationBehavior<TCommand, TResult>> _logger;

    public ValidationBehavior(
        ICommandHandler<TCommand, TResult> next,
        ILogger<ValidationBehavior<TCommand, TResult>> logger,
        IValidator<TCommand>? validator = null)
    {
        _next = next;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        if (_validator is not null)
        {
            _logger.LogDebug("Validating {CommandType}", typeof(TCommand).Name);

            var validationResult = await _validator.ValidateAsync(command, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for {CommandType}: {Errors}", typeof(TCommand).Name, errors);
                return Result<TResult>.ValidationFailure(errors);
            }
        }

        return await _next.HandleAsync(command, cancellationToken);
    }
}

public class LoggingBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _next;
    private readonly ILogger<LoggingBehavior<TCommand, TResult>> _logger;

    public LoggingBehavior(
        ICommandHandler<TCommand, TResult> next,
        ILogger<LoggingBehavior<TCommand, TResult>> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var commandName = typeof(TCommand).Name;
        _logger.LogInformation("Executing {CommandName}", commandName);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _next.HandleAsync(command, cancellationToken);
        stopwatch.Stop();

        if (result.IsSuccess)
            _logger.LogInformation("{CommandName} completed in {ElapsedMs}ms", commandName, stopwatch.ElapsedMilliseconds);
        else
            _logger.LogWarning("{CommandName} failed in {ElapsedMs}ms: {Error}", commandName, stopwatch.ElapsedMilliseconds, result.Error.Message);

        return result;
    }
}
