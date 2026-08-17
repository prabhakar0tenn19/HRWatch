using HRWatch.Domain.Common;

namespace HRWatch.Domain.Entities;

public class EmployeeException : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "HR";
    public bool IsActive { get; set; } = true; // Soft delete / revocation
}
