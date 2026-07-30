using HRWatch.Domain.Common;
using HRWatch.Domain.Enums;

namespace HRWatch.Domain.Entities;

/// <summary>
/// A recorded violation against an employee.
/// Created by the ComplianceEvaluator domain service when a policy rule is broken.
/// </summary>
public class Violation : AuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    public ViolationType Type { get; private set; }

    /// <summary>Human-readable description of what rule was broken and by how much</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>The date the violation occurred</summary>
    public DateTime OccurredOn { get; private set; }

    /// <summary>Whether HR has reviewed and acknowledged this violation</summary>
    public bool IsAcknowledged { get; private set; }

    public DateTime? AcknowledgedAt { get; private set; }
    public string? AcknowledgedBy  { get; private set; }

    /// <summary>Link to the policy that was violated</summary>
    public Guid? PolicyId { get; private set; }
    public Policy? Policy { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static Violation Create(
        Guid employeeId,
        ViolationType type,
        string description,
        DateTime occurredOn,
        Guid? policyId = null,
        string createdBy = "system") => new()
    {
        EmployeeId  = employeeId,
        Type        = type,
        Description = description,
        OccurredOn  = occurredOn,
        PolicyId    = policyId,
        CreatedBy   = createdBy,
        CreatedAt   = DateTime.UtcNow
    };

    // ── Behavior ─────────────────────────────────────────────────────────────

    public void Acknowledge(string acknowledgedBy)
    {
        IsAcknowledged  = true;
        AcknowledgedAt  = DateTime.UtcNow;
        AcknowledgedBy  = acknowledgedBy;
        UpdatedAt       = DateTime.UtcNow;
        UpdatedBy       = acknowledgedBy;
    }

    private Violation() { }
}
