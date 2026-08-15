using HRWatch.Application.Features.Employees.DTOs;
using HRWatch.Application.Features.Attendance.DTOs;

namespace HRWatch.Application.Common.Abstractions;

public interface IEmployeeWeeklyOverviewApiClient
{
    Task<IReadOnlyList<ExternalEmployeeWeeklyOverviewDto>> GetWeeklyOverviewAsync(
        CancellationToken cancellationToken = default);
}

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
