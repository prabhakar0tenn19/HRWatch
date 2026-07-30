using HRWatch.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace HRWatch.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, message) = exception switch
        {
            NotFoundException nfe => (HttpStatusCode.NotFound, "NOT_FOUND", nfe.Message),
            DomainException de => (HttpStatusCode.BadRequest, de.ErrorCode, de.Message),
            ExternalApiException eae => (HttpStatusCode.BadGateway, "EXTERNAL_API_ERROR", eae.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "UNAUTHORIZED", "Access denied."),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred. Please try again later.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning("Handled exception [{Code}]: {Message}", errorCode, exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse(
            StatusCode: (int)statusCode,
            ErrorCode: errorCode,
            Message: message,
            Details: _environment.IsDevelopment() ? exception.ToString() : null,
            TraceId: context.TraceIdentifier);

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonOptions));
    }
}

public record ErrorResponse(
    int StatusCode,
    string ErrorCode,
    string Message,
    string? Details,
    string? TraceId);
