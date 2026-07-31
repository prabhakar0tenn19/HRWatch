using HRWatch.Domain.Common;

namespace HRWatch.Domain.Entities;

public class Employee : AggregateRoot
{
    public string ExternalId { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }

    public Guid? DesignationId { get; private set; }
    public Designation? Designation { get; private set; }

    public string Department { get; private set; } = string.Empty;

    public Guid? ManagerId { get; private set; }
    public Employee? Manager { get; private set; }

    public DateTime JoinDate { get; private set; }
    public DateTime? TerminationDate { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<Attendance> _attendanceRecords = [];
    public IReadOnlyList<Attendance> AttendanceRecords => _attendanceRecords.AsReadOnly();

    private readonly List<Violation> _violations = [];
    public IReadOnlyList<Violation> Violations => _violations.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}";
    public bool IsCurrentlyEmployed => IsActive && TerminationDate is null;

    private Employee() { }

    public static Employee Create(
        string externalId,
        string firstName,
        string lastName,
        string email,
        string department,
        DateTime joinDate,
        string createdBy = "system")
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));

        var employee = new Employee
        {
            ExternalId = externalId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Department = department,
            JoinDate = joinDate,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        employee.RaiseDomainEvent(new EmployeeJoinedDomainEvent(employee.Id, email, joinDate));
        return employee;
    }

    public void UpdateFromExternalApi(string firstName, string lastName, string department, string updatedBy)
    {
        FirstName = firstName;
        LastName = lastName;
        Department = department;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(DateTime terminationDate, string updatedBy)
    {
        IsActive = false;
        TerminationDate = terminationDate;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void AssignManager(Guid managerId)
    {
        ManagerId = managerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignDesignation(Guid designationId)
    {
        DesignationId = designationId;
        UpdatedAt = DateTime.UtcNow;
    }
}

public record EmployeeJoinedDomainEvent(
    Guid EmployeeId,
    string Email,
    DateTime JoinDate) : IDomainEvent;
