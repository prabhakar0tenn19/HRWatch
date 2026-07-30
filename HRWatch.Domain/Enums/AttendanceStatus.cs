namespace HRWatch.Domain.Enums;

/// <summary>
/// Represents what happened with an employee's attendance for a given day.
/// </summary>
public enum AttendanceStatus
{
    /// <summary>Employee was present and on time</summary>
    Present = 1,

    /// <summary>Employee was absent without approval</summary>
    Absent = 2,

    /// <summary>Employee arrived late (past the grace period defined in policy)</summary>
    Late = 3,

    /// <summary>Employee left earlier than required hours</summary>
    EarlyLeave = 4,

    /// <summary>Employee is on approved leave</summary>
    OnLeave = 5,

    /// <summary>Public holiday — attendance not required</summary>
    Holiday = 6,

    /// <summary>Weekend — attendance not required</summary>
    Weekend = 7,

    /// <summary>Employee worked from home (if WFH policy enabled)</summary>
    WorkFromHome = 8
}
