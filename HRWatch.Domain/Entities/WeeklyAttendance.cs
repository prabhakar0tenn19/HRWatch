using HRWatch.Domain.Common;

namespace HRWatch.Domain.Entities;

public class WeeklyAttendance : AuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    public DateTime WeekStartDate { get; private set; }
    public DateTime WeekEndDate { get; private set; }

    public int PresentCount { get; private set; }
    public int LeaveCount { get; private set; }
    public string RawLeaveJson { get; private set; } = "[]";

    private WeeklyAttendance() { }

    public static WeeklyAttendance Create(
        Guid employeeId,
        DateTime weekStartDate,
        DateTime weekEndDate,
        int presentCount,
        int leaveCount,
        string rawLeaveJson,
        string createdBy = "system")
    {
        return new WeeklyAttendance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            WeekStartDate = weekStartDate.Date,
            WeekEndDate = weekEndDate.Date,
            PresentCount = presentCount,
            LeaveCount = leaveCount,
            RawLeaveJson = rawLeaveJson,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateAttendance(int presentCount, int leaveCount, string rawLeaveJson, string updatedBy = "system")
    {
        PresentCount = presentCount;
        LeaveCount = leaveCount;
        RawLeaveJson = rawLeaveJson;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
