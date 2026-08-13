using HRWatch.Domain.Common;
using HRWatch.Domain.Enums;
using HRWatch.Domain.ValueObjects;

namespace HRWatch.Domain.Entities;


/// Represents a single attendance record for an employee on a specific date.
/// Each record comes from the external Attendance API and is processed by the sync job.

public class Attendance : AuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    public DateTime Date { get; private set; }

    /// Actual clock-in time. Null if employee was absent/holiday.
    public TimeSpan? CheckIn { get; private set; }

    /// Actual clock-out time. Null if employee was absent/holiday.
    public TimeSpan? CheckOut { get; private set; }

    public AttendanceStatus Status { get; private set; }

    /// Total work hours computed from CheckIn/CheckOut using WorkHours value object
    public decimal? TotalWorkHours { get; private set; }

    /// Remarks from the HR system or computed by compliance engine
    public string? Remarks { get; private set; }

    /// External reference ID from the attendance API
    public string? ExternalReferenceId { get; private set; }

    // ── Factory Method 
    public static Attendance Create(
        Guid employeeId,
        DateTime date,
        TimeSpan? checkIn,
        TimeSpan? checkOut,
        AttendanceStatus status,
        string? externalReferenceId = null,
        string createdBy = "system")
    {
        var attendance = new Attendance
        {
            EmployeeId          = employeeId,
            Date                = date.Date, // strip time component — one record per day
            CheckIn             = checkIn,
            CheckOut            = checkOut,
            Status              = status,
            ExternalReferenceId = externalReferenceId,
            CreatedBy           = createdBy,
            CreatedAt           = DateTime.UtcNow
        };

        // Use value object to compute total work hours
        if (checkIn.HasValue && checkOut.HasValue)
        {
            var workHours = WorkHours.Calculate(checkIn.Value, checkOut.Value);
            attendance.TotalWorkHours = workHours.TotalHours;
        }

        return attendance;
    }

    // ── Behavior Methods 
    public void AddRemarks(string remarks)
    {
        Remarks   = remarks;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(AttendanceStatus newStatus, string updatedBy)
    {
        Status    = newStatus;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    private Attendance() { }
}
