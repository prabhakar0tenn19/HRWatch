namespace HRWatch.Domain.Common;


/// Marker interface for domain events.
/// Domain events represent things that HAPPENED in the business domain.
/// e.g., EmployeeJoinedEvent, AttendanceViolationDetectedEvent
/// These can be dispatched after saving to trigger side effects (notifications, audit logs).

public interface IDomainEvent { }


/// Base for entities that can raise domain events.
/// Handlers read these events after saving and dispatch them as needed.

public abstract class AggregateRoot : AuditableEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents()
        => _domainEvents.Clear();
}
