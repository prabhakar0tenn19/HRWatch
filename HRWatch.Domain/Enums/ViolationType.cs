namespace HRWatch.Domain.Enums;

/// 
/// Types of policy or attendance violations that can be recorded against an employee.
/// 
public enum ViolationType
{
    /// Late arrival beyond grace period
    LateArrival = 1,

    /// Leaving before minimum required hours
    EarlyDeparture = 2,

    /// Absent without leave approval
    UnauthorizedAbsence = 3,

    /// Exceeded allowed late arrivals in a month
    ExcessiveLateArrivals = 4,

    /// Exceeded allowed absences in a period
    ExcessiveAbsences = 5,

    /// Did not meet minimum weekly work hours
    InsufficientWorkHours = 6,

    /// Policy document not acknowledged within deadline
    PolicyAcknowledgmentMissed = 7
}
