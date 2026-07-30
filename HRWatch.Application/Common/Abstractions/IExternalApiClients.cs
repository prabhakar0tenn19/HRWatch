using HRWatch.Application.Features.Employees.DTOs;
using HRWatch.Application.Features.Attendance.DTOs;

namespace HRWatch.Application.Common.Abstractions;

// ════════════════════════════════════════════════════════════════════════════
// EXTERNAL API CLIENT INTERFACES
// These live in Application because:
//   - Application handlers need to CALL these APIs
//   - The actual HTTP implementation lives in Infrastructure
//   - This keeps Application layer free from HttpClient, JSON serialization, etc.
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Contract for the external Employee API client.
/// Infrastructure.ExternalApis.EmployeeClient implements this.
/// </summary>
public interface IEmployeeApiClient
{
    Task<IReadOnlyList<ExternalEmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default);
    Task<ExternalEmployeeDto?> GetEmployeeByIdAsync(string externalId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Contract for the external Attendance API client.
/// </summary>
public interface IAttendanceApiClient
{
    Task<IReadOnlyList<ExternalAttendanceDto>> GetAttendanceAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contract for the external Leave API client.
/// (Placeholder — will be implemented in a future phase)
/// </summary>
public interface ILeaveApiClient
{
    Task<IReadOnlyList<ExternalLeaveDto>> GetLeavesAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contract for the external Holiday API client.
/// </summary>
public interface IHolidayApiClient
{
    Task<IReadOnlyList<ExternalHolidayDto>> GetHolidaysAsync(
        int year,
        string? region = null,
        CancellationToken cancellationToken = default);
}

// ── Placeholder DTOs for future APIs ─────────────────────────────────────────

public record ExternalLeaveDto(
    string EmployeeExternalId,
    DateTime FromDate,
    DateTime ToDate,
    string LeaveType,
    string Status);

public record ExternalHolidayDto(
    string Name,
    DateTime Date,
    string? Region);
