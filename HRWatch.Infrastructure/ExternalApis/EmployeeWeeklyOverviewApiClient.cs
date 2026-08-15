using System.Text.Json;
using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Employees.DTOs;
using Microsoft.Extensions.Logging;

namespace HRWatch.Infrastructure.ExternalApis;

public class EmployeeWeeklyOverviewApiClient : IEmployeeWeeklyOverviewApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmployeeWeeklyOverviewApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EmployeeWeeklyOverviewApiClient(
        HttpClient httpClient,
        ILogger<EmployeeWeeklyOverviewApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExternalEmployeeWeeklyOverviewDto>> GetWeeklyOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calling External API: GET /api/v2.0/EmployeeWeeklyOverview");

        try
        {
            var response = await _httpClient.GetAsync("/api/v2.0/EmployeeWeeklyOverview", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<List<ExternalEmployeeWeeklyOverviewDto>>(json, JsonOptions);

            return result ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling External Employee Weekly Overview API");
            throw;
        }
    }
}
