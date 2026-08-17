using HRWatch.Domain.Common;

namespace HRWatch.Domain.Entities;

public class Employee : BaseEntity
{
    public string EmployeeCode { get; set; } = string.Empty; // "CGI705", "INT259" (Matches COSEC UserID)
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public bool IsDeployed { get; set; } = true; // false = Bench (Requires 5 days WFO)
    public bool IsActive { get; set; } = true;
    public string Location { get; set; } = "India";

    // Navigation collections
    public ICollection<DailyAttendance> Attendances { get; set; } = new List<DailyAttendance>();
    public ICollection<EmployeeException> Exceptions { get; set; } = new List<EmployeeException>();
}
