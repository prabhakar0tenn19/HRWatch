namespace HRWatch.Domain.Enums;


/// Represents what happened with an employee's attendance for a given day.

public enum AttendanceStatus
{
    /// Employee was present and on time
    Present = 1,

    /// Employee was absent without approval
    Absent = 2,

    /// Employee arrived late (past the grace period defined in policy)
    Late = 3,

    /// Employee left earlier than required hours
    EarlyLeave = 4,

    /// Employee is on approved leave
    OnLeave = 5,

    /// Public holiday — attendance not required
    Holiday = 6,

    /// Weekend — attendance not required
    Weekend = 7,

    /// Employee worked from home (if WFH policy enabled)
    WorkFromHome = 8
}
