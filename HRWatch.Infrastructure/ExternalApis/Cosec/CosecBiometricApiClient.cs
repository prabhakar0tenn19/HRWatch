using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using HRWatch.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HRWatch.Infrastructure.ExternalApis.Cosec;

public class CosecBiometricApiClient : ICosecBiometricApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CosecBiometricApiClient> _logger;

    public CosecBiometricApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<CosecBiometricApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = configuration["Cosec:BaseUrl"] ?? "http://172.24.120.88";
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        var username = configuration["Cosec:Username"] ?? "API";
        var password = configuration["Cosec:Password"] ?? "Api@123";
        var authBytes = Encoding.ASCII.GetBytes($"{username}:{password}");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
    }

    public async Task<IReadOnlyList<CosecPunchRecord>> GetPunchesForDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var fromStr = fromDate.ToString("ddMMyyyy000000");
        var toStr = toDate.ToString("ddMMyyyy235959");

        var url = $"/cosec/api.svc/v2/event-ta?action=get;date-range={fromStr}-{toStr};field-name=userid,edate,device_name,etime,entryexittype";

        _logger.LogInformation("Calling Matrix COSEC Biometric API: {Url}", url);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("COSEC API returned status code {StatusCode}", response.StatusCode);
                return [];
            }

            var rawText = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseCosecResponse(rawText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data from COSEC Biometric API");
            return [];
        }
    }

    public async Task<HashSet<string>> GetPresentEmployeeCodesForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        var records = await GetPunchesForDateRangeAsync(dateTime, dateTime, cancellationToken);

        return records
            .Where(r => r.PunchDate == date && !string.IsNullOrWhiteSpace(r.EmployeeCode))
            .Select(r => r.EmployeeCode.Trim().ToUpperInvariant())
            .ToHashSet();
    }

    private static IReadOnlyList<CosecPunchRecord> ParseCosecResponse(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return [];

        var list = new List<CosecPunchRecord>();
        var lines = rawText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("UserID", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = trimmed.Split('|');
            if (parts.Length < 4) continue;

            var userId = parts[0].Trim();
            var dateStr = parts[1].Trim();
            var deviceName = parts[2].Trim();
            var timeStr = parts[3].Trim();
            var entryExitType = parts.Length > 4 && int.TryParse(parts[4].Trim(), out var ee) ? ee : 1;
            var indexNo = parts.Length > 5 ? parts[5].Trim() : null;

            if (DateOnly.TryParseExact(dateStr, ["dd/MM/yyyy", "d/M/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var punchDate) &&
                TimeOnly.TryParseExact(timeStr, ["HH:mm:ss", "H:m:s"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var punchTime))
            {
                list.Add(new CosecPunchRecord(userId, punchDate, punchTime, deviceName, entryExitType, indexNo));
            }
        }

        return list;
    }
}
