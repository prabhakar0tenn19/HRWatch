namespace HRWatch.Domain.Enums;


/// Employee roles within the organization hierarchy.

public enum Role
{
    /// Individual contributor with no management responsibilities
    Employee = 1,

    /// Team lead — manages a small team, still contributes individually
    TeamLead = 2,

    /// Manager — manages employees, responsible for approvals
    Manager = 3,

    /// HR personnel — manages policies, leave, reports
    HRManager = 4,

    /// Department head or director
    Director = 5,

    /// System administrator — platform configuration access
    Admin = 6
}
