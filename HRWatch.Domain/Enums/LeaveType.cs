namespace HRWatch.Domain.Enums;

/// 
/// Types of leave an employee can take.
/// 
public enum LeaveType
{
    /// Earned/privilege leave — accrued over time
    Annual = 1,

    /// Sick leave with medical justification
    Sick = 2,

    /// Unpaid leave — salary deduction applies
    Unpaid = 3,

    /// Maternity leave
    Maternity = 4,

    /// Paternity leave
    Paternity = 5,

    /// Compassionate/bereavement leave
    Compassionate = 6,

    /// Study/exam leave
    Study = 7,

    /// Compensatory leave for working overtime
    Compensatory = 8
}
