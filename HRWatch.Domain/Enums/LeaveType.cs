namespace HRWatch.Domain.Enums;

/// <summary>
/// Types of leave an employee can take.
/// </summary>
public enum LeaveType
{
    /// <summary>Earned/privilege leave — accrued over time</summary>
    Annual = 1,

    /// <summary>Sick leave with medical justification</summary>
    Sick = 2,

    /// <summary>Unpaid leave — salary deduction applies</summary>
    Unpaid = 3,

    /// <summary>Maternity leave</summary>
    Maternity = 4,

    /// <summary>Paternity leave</summary>
    Paternity = 5,

    /// <summary>Compassionate/bereavement leave</summary>
    Compassionate = 6,

    /// <summary>Study/exam leave</summary>
    Study = 7,

    /// <summary>Compensatory leave for working overtime</summary>
    Compensatory = 8
}
