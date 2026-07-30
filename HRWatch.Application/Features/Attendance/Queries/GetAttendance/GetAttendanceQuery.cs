using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Attendance.DTOs;

namespace HRWatch.Application.Features.Attendance.Queries.GetAttendance;


public record GetAttendanceQuery : IQuery<IReadOnlyList<AttendanceDto>>
{
    public Guid?     EmployeeId { get; init; }
    public DateTime  FromDate   { get; init; } = DateTime.UtcNow.AddDays(-7);
    public DateTime  ToDate     { get; init; } = DateTime.UtcNow.Date;
}
