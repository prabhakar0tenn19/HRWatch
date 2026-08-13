using HRWatch.Application.Features.Employees.DTOs;
using HRWatch.Application.Features.Attendance.DTOs;

namespace HRWatch.Application.Common.Abstractions;


public interface IEmployeeApiClient
{
    Task<IReadOnlyList<ExternalEmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default);
    Task<ExternalEmployeeDto?> GetEmployeeByIdAsync(string externalId, CancellationToken cancellationToken = default);
}


public interface IAttendanceApiClient
{
    Task<IReadOnlyList<ExternalAttendanceDto>> GetAttendanceAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default);
}


public interface ILeaveApiClient
{
    Task<IReadOnlyList<ExternalLeaveDto>> GetLeavesAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}


public interface IHolidayApiClient
{
    Task<IReadOnlyList<ExternalHolidayDto>> GetHolidaysAsync(
        int year,
        string? region = null,
        CancellationToken cancellationToken = default);
}

// Placeholder DTOs for future APIs

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
