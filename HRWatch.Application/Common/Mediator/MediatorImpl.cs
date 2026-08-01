using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Common.Mediator;

public class CommandMediator : ICommandMediator
{
    private readonly IServiceProvider _serviceProvider;

    public CommandMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<TResult>> SendAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();

        // 1. Resolve actual Handler from DI (e.g. CreateEmployeeCommandHandler)
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        var rawHandler = _serviceProvider.GetRequiredService(handlerType);

        // 2. Resolve optional FluentValidation validator
        var validatorType = typeof(IValidator<>).MakeGenericType(commandType);
        var validator = _serviceProvider.GetService(validatorType);

        // 3. Wrap rawHandler with ValidationBehavior
        var validationBehaviorType = typeof(ValidationBehavior<,>).MakeGenericType(commandType, typeof(TResult));
        var validationLoggerType = typeof(ILogger<>).MakeGenericType(validationBehaviorType);
        var validationLogger = _serviceProvider.GetRequiredService(validationLoggerType);

        var validationBehavior = Activator.CreateInstance(
            validationBehaviorType, rawHandler, validationLogger, validator)!;

        // 4. Wrap validationBehavior with LoggingBehavior (Outer Decorator)
        var loggingBehaviorType = typeof(LoggingBehavior<,>).MakeGenericType(commandType, typeof(TResult));
        var loggingLoggerType = typeof(ILogger<>).MakeGenericType(loggingBehaviorType);
        var loggingLogger = _serviceProvider.GetRequiredService(loggingLoggerType);

        var loggingBehavior = Activator.CreateInstance(
            loggingBehaviorType, validationBehavior, loggingLogger)!;

        // 5. Mediator DISPATCHES to top decorator (LoggingBehavior.HandleAsync)
        var handleMethod = loggingBehaviorType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.HandleAsync))!;
        var task = (Task<Result<TResult>>)handleMethod.Invoke(loggingBehavior, [command, cancellationToken])!;

        return await task;
    }
}

public class QueryMediator : IQueryMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueryMediator> _logger;

    public QueryMediator(IServiceProvider serviceProvider, ILogger<QueryMediator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Result<TResult>> SendAsync<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        var queryType = query.GetType();
        _logger.LogDebug("Dispatching query: {QueryType}", queryType.Name);

        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.HandleAsync))!;

        try
        {
            var task = (Task<Result<TResult>>)method.Invoke(handler, [query, cancellationToken])!;
            return await task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in query handler for {QueryType}", queryType.Name);
            throw;
        }
    }
}
