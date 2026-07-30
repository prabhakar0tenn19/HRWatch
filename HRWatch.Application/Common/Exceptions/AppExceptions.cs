namespace HRWatch.Application.Common.Exceptions;


public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }
}



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
