namespace HRWatch.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string PerformedBy { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public string? Details { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string action,
        string entityName,
        string entityId,
        string performedBy,
        string? details = null)
    {
        if (string.IsNullOrWhiteSpace(action)) throw new ArgumentException("Action is required.", nameof(action));
        if (string.IsNullOrWhiteSpace(entityName)) throw new ArgumentException("Entity name is required.", nameof(entityName));

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            PerformedBy = performedBy,
            Timestamp = DateTime.UtcNow,
            Details = details
        };
    }
}
