using System.Text.Json.Serialization;

namespace HRWatch.Application.Common.Interfaces;

public record Cg1EmployeeDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("employeeCode")] string? EmployeeCode,
    [property: JsonPropertyName("location")] string? Location,
    [property: JsonPropertyName("designation")] string Designation,
    [property: JsonPropertyName("startDate")] DateTime StartDate,
    [property: JsonPropertyName("endDate")] DateTime EndDate,
    [property: JsonPropertyName("leave")] List<string>? Leave,
    [property: JsonPropertyName("isDeployed")] bool IsDeployed);

public interface ICg1ApiClient
{
    Task<IReadOnlyList<Cg1EmployeeDto>> GetMasterEmployeesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cg1EmployeeDto>> GetLeavesByEmailsAsync(IEnumerable<string> emails, DateOnly date, CancellationToken cancellationToken = default);
}
