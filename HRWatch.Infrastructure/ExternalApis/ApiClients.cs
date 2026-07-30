using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Common.Exceptions;
using HRWatch.Application.Features.Employees.DTOs;
using HRWatch.Application.Features.Attendance.DTOs;
using HRWatch.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace HRWatch.Infrastructure.ExternalApis;


public class ExternalApiOptions
{
    public ApiEndpointConfig EmployeeApi   { get; init; } = new();
    public ApiEndpointConfig AttendanceApi { get; init; } = new();
    public ApiEndpointConfig LeaveApi      { get; init; } = new();
    public ApiEndpointConfig HolidayApi    { get; init; } = new();
}

public class ApiEndpointConfig
{
    public string BaseUrl { get; init; } = string.Empty;
    public string? ApiKey { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
}

// ════════════════════════════════════════════════════════════════════════════
// EMPLOYEE API CLIENT
// ════════════════════════════════════════════════════════════════════════════


/// External Employee API Client.
/// 
/// RESPONSIBILITIES:
///   - Making HTTP calls to the external Employee API
///   - Deserializing responses
///   - Throwing ExternalApiException on failures
/// 
/// NOT RESPONSIBLE FOR:
///   - Business logic (no if/else about what to do with the data)
///   - Database access
///   - Anything other than HTTP communication
/// 
/// This is registered with Named HttpClient in DI ("EmployeeApiClient")
/// which handles base URL, timeout, and retry policies.

public class EmployeeClient : IEmployeeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmployeeClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EmployeeClient(HttpClient httpClient, ILogger<EmployeeClient> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    public async Task<IReadOnlyList<ExternalEmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calling Employee API: GET /employees");

        try
        {
            var response = await _httpClient.GetAsync("/employees", cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new ExternalApiException("EmployeeApi",
                    $"GET /employees returned {(int)response.StatusCode}", (int)response.StatusCode);

            var json    = await response.Content.ReadAsStringAsync(cancellationToken);
            var result  = JsonSerializer.Deserialize<List<ExternalEmployeeDto>>(json, JsonOptions);
            return result ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Employee API is unreachable");
            throw new ExternalApiException("EmployeeApi", $"HTTP request failed: {ex.Message}");
        }
    }

    public async Task<ExternalEmployeeDto?> GetEmployeeByIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calling Employee API: GET /employees/{ExternalId}", externalId);

        try
        {
            var response = await _httpClient.GetAsync($"/employees/{externalId}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode)
                throw new ExternalApiException("EmployeeApi",
                    $"GET /employees/{externalId} returned {(int)response.StatusCode}", (int)response.StatusCode);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ExternalEmployeeDto>(json, JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Employee API error for ExternalId {Id}", externalId);
            throw new ExternalApiException("EmployeeApi", ex.Message);
        }
    }
}

// ════════════════════════════════════════════════════════════════════════════
// ATTENDANCE API CLIENT
// ════════════════════════════════════════════════════════════════════════════

public class AttendanceClient : IAttendanceApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AttendanceClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AttendanceClient(HttpClient httpClient, ILogger<AttendanceClient> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    public async Task<IReadOnlyList<ExternalAttendanceDto>> GetAttendanceAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/attendance?from={fromDate:yyyy-MM-dd}&to={toDate:yyyy-MM-dd}";
        if (employeeId.HasValue) url += $"&employeeId={employeeId}";

        _logger.LogDebug("Calling Attendance API: GET {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new ExternalApiException("AttendanceApi",
                    $"GET {url} returned {(int)response.StatusCode}", (int)response.StatusCode);

            var json   = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<List<ExternalAttendanceDto>>(json, JsonOptions);
            return result ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Attendance API is unreachable");
            throw new ExternalApiException("AttendanceApi", ex.Message);
        }
    }
}

// ════════════════════════════════════════════════════════════════════════════
// LEAVE API CLIENT (Placeholder for future)
// ════════════════════════════════════════════════════════════════════════════

public class LeaveClient : ILeaveApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LeaveClient> _logger;

    public LeaveClient(HttpClient httpClient, ILogger<LeaveClient> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    public async Task<IReadOnlyList<ExternalLeaveDto>> GetLeavesAsync(
        DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("LeaveClient is a placeholder — Leave API not yet integrated.");
        return await Task.FromResult<IReadOnlyList<ExternalLeaveDto>>([]);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// HOLIDAY API CLIENT (Placeholder for future)
// ════════════════════════════════════════════════════════════════════════════

public class HolidayClient : IHolidayApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HolidayClient> _logger;

    public HolidayClient(HttpClient httpClient, ILogger<HolidayClient> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    public async Task<IReadOnlyList<ExternalHolidayDto>> GetHolidaysAsync(
        int year, string? region = null, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("HolidayClient is a placeholder — Holiday API not yet integrated.");
        return await Task.FromResult<IReadOnlyList<ExternalHolidayDto>>([]);
    }
}
