namespace HRWatch.Application.Common.Abstractions;

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static Error NotFound(string entityName, object key) => new("NOT_FOUND", $"{entityName} with key '{key}' was not found.");
    public static Error Validation(string message) => new("VALIDATION_ERROR", message);
    public static Error Failure(string code, string message) => new(code, message);
}

public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error Error { get; }

    private Result(bool isSuccess, T? value, Error error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, Error.None);
    public static Result<T> Failure(Error error) => new(false, default, error);
    public static Result<T> Failure(string code, string message) => new(false, default, new Error(code, message));
    public static Result<T> NotFound(string entityName, object key) => new(false, default, Error.NotFound(entityName, key));
    public static Result<T> ValidationFailure(string message) => new(false, default, Error.Validation(message));
}

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(string code, string message) => new(false, new Error(code, message));
    public static Result NotFound(string entityName, object key) => new(false, Error.NotFound(entityName, key));
    public static Result ValidationFailure(string message) => new(false, Error.Validation(message));

    public static Result<Unit> Ok() => Result<Unit>.Success(Unit.Value);
    public static Result<Unit> Fail(string code, string message) => Result<Unit>.Failure(code, message);
}
