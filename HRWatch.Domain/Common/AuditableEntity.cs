namespace HRWatch.Domain.Common;


/// Base class for all entities that need audit tracking.
/// Every table should know WHO created/updated it and WHEN.
/// Domain stays pure — no EF Core attributes here, Fluent API handles the DB mapping.

public abstract class AuditableEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// Who created this record (employee ID or system job name)
    public string CreatedBy { get; set; } = "system";

    /// Who last updated this record
    public string? UpdatedBy { get; set; }
}
