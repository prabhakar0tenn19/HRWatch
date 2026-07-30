using HRWatch.Application.Common;
using HRWatch.Application.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Common.Mediator;

public class CommandMediator : ICommandMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandMediator> _logger;

    public CommandMediator(IServiceProvider serviceProvider, ILogger<CommandMediator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Result<TResult>> SendAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();
        _logger.LogDebug("Dispatching command: {CommandType}", commandType.Name);

        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.HandleAsync))!;

        try
        {
            var task = (Task<Result<TResult>>)method.Invoke(handler, [command, cancellationToken])!;
            return await task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in command handler for {CommandType}", commandType.Name);
            throw;
        }
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
