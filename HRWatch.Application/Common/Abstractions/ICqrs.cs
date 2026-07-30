using HRWatch.Application.Common;

namespace HRWatch.Application.Common.Abstractions;

public interface ICommand : ICommand<Unit> { }

public interface ICommand<out TResult> { }

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<Result<Unit>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface IQuery<out TResult> { }

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

public interface ICommandMediator
{
    Task<Result<TResult>> SendAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default);
}

public interface IQueryMediator
{
    Task<Result<TResult>> SendAsync<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default);
}

public readonly struct Unit
{
    public static readonly Unit Value = new();
    public override string ToString() => "()";
}
