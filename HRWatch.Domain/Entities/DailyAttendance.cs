using HRWatch.Domain.Common;
using HRWatch.Domain.Enums;

namespace HRWatch.Domain.Entities;

public class DailyAttendance : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; } // P, L, E, A, WO, H
    public string? LeaveType { get; set; } // Casual Leave, Sick Leave, Earned Leave

    public Guid RuleVersionId { get; set; }
    public Policy? Policy { get; set; }
}
