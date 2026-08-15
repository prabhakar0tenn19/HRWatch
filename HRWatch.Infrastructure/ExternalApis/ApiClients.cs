using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Common.Exceptions;
using HRWatch.Application.Features.Employees.DTOs;
using HRWatch.Application.Features.Attendance.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HRWatch.Infrastructure.ExternalApis;

public class EmployeeClient : IEmployeeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmployeeClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public EmployeeClient(HttpClient httpClient, ILogger<EmployeeClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExternalEmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/employees", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ExternalApiException("EmployeeApi", $"GET /employees returned {(int)response.StatusCode}", (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<ExternalEmployeeDto>>(json, JsonOptions) ?? [];
    }

    public async Task<ExternalEmployeeDto?> GetEmployeeByIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/employees/{externalId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode)
            throw new ExternalApiException("EmployeeApi", $"GET /employees/{externalId} returned {(int)response.StatusCode}", (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<ExternalEmployeeDto>(json, JsonOptions);
    }
}

public class AttendanceClient : IAttendanceApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AttendanceClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AttendanceClient(HttpClient httpClient, ILogger<AttendanceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExternalAttendanceDto>> GetAttendanceAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/attendance?from={fromDate:yyyy-MM-dd}&to={toDate:yyyy-MM-dd}";
        if (employeeId.HasValue) url += $"&employeeId={employeeId}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ExternalApiException("AttendanceApi", $"GET {url} returned {(int)response.StatusCode}", (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<ExternalAttendanceDto>>(json, JsonOptions) ?? [];
    }
}
