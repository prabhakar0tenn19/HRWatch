using HRWatch.Domain.Common;
using HRWatch.Domain.Enums;

namespace HRWatch.Domain.Entities;

/// <summary>
/// Core domain entity representing an employee.
/// This is an Aggregate Root — all employee-related changes go through here.
/// 
/// NOTE: No EF Core attributes ([Key], [Column] etc.) — all mapping is done
/// in Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs using Fluent API.
/// </summary>
public class Employee : AggregateRoot
{
    // ── Identity ────────────────────────────────────────────────────────────
    
    /// <summary>External ID from the Employee API (source system)</summary>
    public string ExternalId { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;
    public string LastName  { get; private set; } = string.Empty;
    public string Email     { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }

    // ── Organization ────────────────────────────────────────────────────────

    public Guid? DesignationId { get; private set; }
    public Designation? Designation { get; private set; }

    /// <summary>Department name — stored as string for flexibility</summary>
    public string Department { get; private set; } = string.Empty;

    public Role Role { get; private set; }

    /// <summary>Direct manager — nullable because Directors have no manager</summary>
    public Guid? ManagerId { get; private set; }
    public Employee? Manager { get; private set; }

    // ── Employment ──────────────────────────────────────────────────────────

    public DateTime JoinDate { get; private set; }
    public DateTime? TerminationDate { get; private set; }
    public bool IsActive { get; private set; } = true;

    // ── Navigation properties ────────────────────────────────────────────────

    private readonly List<Attendance> _attendanceRecords = [];
    public IReadOnlyList<Attendance> AttendanceRecords => _attendanceRecords.AsReadOnly();

    private readonly List<Violation> _violations = [];
    public IReadOnlyList<Violation> Violations => _violations.AsReadOnly();

    // ── Computed Properties (pure business logic) ────────────────────────────

    public string FullName => $"{FirstName} {LastName}";

    public bool IsCurrentlyEmployed => IsActive && TerminationDate is null;

    // ── Factory Method ───────────────────────────────────────────────────────

    /// <summary>
    /// Use this static factory instead of a public constructor.
    /// Ensures an Employee is always created in a valid state.
    /// This is where domain invariants (business rules) would be enforced.
    /// </summary>
    public static Employee Create(
        string externalId,
        string firstName,
        string lastName,
        string email,
        string department,
        Role role,
        DateTime joinDate,
        string createdBy = "system")
    {
        // Domain Invariant: an employee MUST have a name and email
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(email))     throw new ArgumentException("Email is required.", nameof(email));

        var employee = new Employee
        {
            ExternalId  = externalId,
            FirstName   = firstName,
            LastName    = lastName,
            Email       = email,
            Department  = department,
            Role        = role,
            JoinDate    = joinDate,
            CreatedBy   = createdBy,
            CreatedAt   = DateTime.UtcNow
        };

        // Raise a domain event — other parts of the system can react to this
        // (e.g., send welcome email, create onboarding task)
        employee.RaiseDomainEvent(new EmployeeJoinedDomainEvent(employee.Id, email, joinDate));

        return employee;
    }

    // ── Behavior Methods ─────────────────────────────────────────────────────

    /// <summary>Update employee details during sync from external Employee API</summary>
    public void UpdateFromExternalApi(string firstName, string lastName, string department, Role role, string updatedBy)
    {
        FirstName   = firstName;
        LastName    = lastName;
        Department  = department;
        Role        = role;
        UpdatedAt   = DateTime.UtcNow;
        UpdatedBy   = updatedBy;
    }

    public void Deactivate(DateTime terminationDate, string updatedBy)
    {
        IsActive          = false;
        TerminationDate   = terminationDate;
        UpdatedAt         = DateTime.UtcNow;
        UpdatedBy         = updatedBy;
    }

    public void AssignManager(Guid managerId)
    {
        ManagerId = managerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignDesignation(Guid designationId)
    {
        DesignationId = designationId;
        UpdatedAt     = DateTime.UtcNow;
    }

    // EF Core needs a parameterless constructor for materialization
    private Employee() { }
}

// ── Domain Event ─────────────────────────────────────────────────────────────

/// <summary>
/// Raised when a new employee joins the system.
/// Handlers can send welcome emails, create IT tickets, etc.
/// </summary>
public record EmployeeJoinedDomainEvent(
    Guid EmployeeId,
    string Email,
    DateTime JoinDate) : IDomainEvent;
