namespace HRWatch.Domain.Enums;

/// <summary>
/// Types of policy or attendance violations that can be recorded against an employee.
/// </summary>
public enum ViolationType
{
    /// <summary>Late arrival beyond grace period</summary>
    LateArrival = 1,

    /// <summary>Leaving before minimum required hours</summary>
    EarlyDeparture = 2,

    /// <summary>Absent without leave approval</summary>
    UnauthorizedAbsence = 3,

    /// <summary>Exceeded allowed late arrivals in a month</summary>
    ExcessiveLateArrivals = 4,

    /// <summary>Exceeded allowed absences in a period</summary>
    ExcessiveAbsences = 5,

    /// <summary>Did not meet minimum weekly work hours</summary>
    InsufficientWorkHours = 6,

    /// <summary>Policy document not acknowledged within deadline</summary>
    PolicyAcknowledgmentMissed = 7
}
