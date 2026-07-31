using HRWatch.Domain.Common;
using HRWatch.Domain.Enums;

namespace HRWatch.Domain.Entities;

public class Violation : AuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    public ViolationType Type { get; private set; }
    public ViolationSeverity Severity { get; private set; }

    public string Description { get; private set; } = string.Empty;
    public DateTime OccurredOn { get; private set; }

    public bool IsAcknowledged { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public string? AcknowledgedBy { get; private set; }

    public Guid? PolicyId { get; private set; }
    public Policy? Policy { get; private set; }

    private Violation() { }

    public static Violation Create(
        Guid employeeId,
        ViolationType type,
        ViolationSeverity severity,
        string description,
        DateTime occurredOn,
        Guid? policyId = null,
        string createdBy = "system")
    {
        return new Violation
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Type = type,
            Severity = severity,
            Description = description,
            OccurredOn = occurredOn.Date,
            PolicyId = policyId,
            IsAcknowledged = false,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Acknowledge(string acknowledgedBy)
    {
        IsAcknowledged = true;
        AcknowledgedAt = DateTime.UtcNow;
        AcknowledgedBy = acknowledgedBy;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = acknowledgedBy;
    }
}
