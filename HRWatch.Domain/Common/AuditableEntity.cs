namespace HRWatch.Domain.Common;

/// <summary>
/// Base class for all entities that need audit tracking.
/// Every table should know WHO created/updated it and WHEN.
/// Domain stays pure — no EF Core attributes here, Fluent API handles the DB mapping.
/// </summary>
public abstract class AuditableEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Who created this record (employee ID or system job name)</summary>
    public string CreatedBy { get; set; } = "system";

    /// <summary>Who last updated this record</summary>
    public string? UpdatedBy { get; set; }
}
