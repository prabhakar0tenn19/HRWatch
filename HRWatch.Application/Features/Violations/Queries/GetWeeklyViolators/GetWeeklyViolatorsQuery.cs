using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Violations.Queries.GetWeeklyViolators;

public record GetWeeklyViolatorsQuery(
    DateTime? WeekStartDate = null,
    string? Designation = null,
    string? Department = null
) : IQuery<IReadOnlyList<WeeklyViolatorDto>>;

public record WeeklyViolatorDto(
    Guid ViolationId,
    Guid EmployeeId,
    string EmployeeName,
    string Email,
    string Designation,
    string Department,
    DateTime OccurredOn,
    string Type,
    string Severity,
    string Description,
    bool IsAcknowledged);
