namespace HRWatch.Domain.Enums;

/// <summary>
/// Employee roles within the organization hierarchy.
/// </summary>
public enum Role
{
    /// <summary>Individual contributor with no management responsibilities</summary>
    Employee = 1,

    /// <summary>Team lead — manages a small team, still contributes individually</summary>
    TeamLead = 2,

    /// <summary>Manager — manages employees, responsible for approvals</summary>
    Manager = 3,

    /// <summary>HR personnel — manages policies, leave, reports</summary>
    HRManager = 4,

    /// <summary>Department head or director</summary>
    Director = 5,

    /// <summary>System administrator — platform configuration access</summary>
    Admin = 6
}
