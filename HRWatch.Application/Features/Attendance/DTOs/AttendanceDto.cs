using HRWatch.Application.Common.Abstractions;
using HRWatch.Domain.Enums;

namespace HRWatch.Application.Features.Attendance.DTOs;


public record AttendanceDto
{
    public Guid             Id            { get; init; }
    public Guid             EmployeeId    { get; init; }
    public string           EmployeeName  { get; init; } = string.Empty;
    public DateTime         Date          { get; init; }
    public TimeSpan?        CheckIn       { get; init; }
    public TimeSpan?        CheckOut      { get; init; }
    public AttendanceStatus Status        { get; init; }
    public string           StatusDisplay { get; init; } = string.Empty;
    public decimal?         TotalWorkHours { get; init; }
    public string?          Remarks       { get; init; }
}

public record ExternalAttendanceDto
{
    public string           EmployeeExternalId  { get; init; } = string.Empty;
    public DateTime         Date                { get; init; }
    public TimeSpan?        CheckIn             { get; init; }
    public TimeSpan?        CheckOut            { get; init; }
    public AttendanceStatus Status              { get; init; }
    public string?          ExternalReferenceId { get; init; }
}
