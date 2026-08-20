using System.Text.Json;
using HRWatch.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HRWatch.Infrastructure.ExternalApis.Cg1;

public class Cg1ApiClient : ICg1ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Cg1ApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Cg1ApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<Cg1ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = configuration["CG1:BaseUrl"] ?? "https://localhost:5092";
        var secretKey = configuration["CG1:SecretKey"] ?? "__EmployeeWeeklyOverviewSecretKey__";

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("Secret-Key") && !string.IsNullOrWhiteSpace(secretKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Secret-Key", secretKey);
        }
    }

    public async Task<IReadOnlyList<Cg1EmployeeDto>> GetMasterEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var url = "/api/v2/EmployeeWeeklyOverview";
        _logger.LogInformation("Calling CG1 Master Employee API: {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CG1 Master API returned status {StatusCode}", response.StatusCode);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var allEmployees = JsonSerializer.Deserialize<List<Cg1EmployeeDto>>(json, JsonOptions) ?? [];

            // Filter for India (Offshore) employees only
            var indiaEmployees = allEmployees
                .Where(e => string.Equals(e.Location?.Trim(), "Offshore", StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInformation("CG1 Master API returned {Total} total employees, filtered {Count} India (Offshore) employees",
                allEmployees.Count, indiaEmployees.Count);

            return indiaEmployees;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call CG1 Master Employee API");
            return [];
        }
    }

    public async Task<IReadOnlyList<Cg1EmployeeDto>> GetLeavesByEmailsAsync(
        IEnumerable<string> emails,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var emailList = emails.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList();
        if (emailList.Count == 0)
        {
            return [];
        }

        var dateStart = $"{date:yyyy-MM-dd}T00:00:00";
        var dateEnd = $"{date:yyyy-MM-dd}T23:59:59";

        var queryParams = string.Join("&", emailList.Select(e => $"emailIds={Uri.EscapeDataString(e.Trim())}"));
        var url = $"/api/v2/EmployeeWeeklyOverview/by-emails?{queryParams}&startDate={dateStart}&endDate={dateEnd}";

        _logger.LogInformation("Calling CG1 Leave by-emails API for {Count} potential violators on {Date}", emailList.Count, date);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CG1 by-emails API returned status {StatusCode}", response.StatusCode);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<Cg1EmployeeDto>>(json, JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call CG1 Leave by-emails API");
            return [];
        }
    }
}
