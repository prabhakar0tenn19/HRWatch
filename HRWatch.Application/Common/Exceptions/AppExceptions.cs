namespace HRWatch.Application.Common.Exceptions;

/// <summary>
/// Thrown when an entity is not found in the system.
/// NOTE: For handlers returning Result&lt;T&gt;, prefer Result.NotFound() over throwing this.
/// Use this only in critical paths where the exception is truly unexpected.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }
}

/// <summary>
/// Thrown for business rule violations that are completely unexpected
/// (not the happy-path failure covered by Result pattern).
/// </summary>
public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode = "DOMAIN_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Thrown when an external API call fails.
/// </summary>
public class ExternalApiException : Exception
{
    public string ApiName  { get; }
    public int?   StatusCode { get; }

    public ExternalApiException(string apiName, string message, int? statusCode = null)
        : base($"[{apiName}] {message}")
    {
        ApiName    = apiName;
        StatusCode = statusCode;
    }
}
