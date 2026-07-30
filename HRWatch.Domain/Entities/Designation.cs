using HRWatch.Domain.Common;

namespace HRWatch.Domain.Entities;

/// <summary>
/// Job title / designation within the organization.
/// Example: "Software Engineer", "Senior HR Manager", "Department Head"
/// </summary>
public class Designation : AuditableEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>Grade level for compensation bands (e.g., L1, L2, L3)</summary>
    public string? GradeLevel { get; private set; }

    public bool IsActive { get; private set; } = true;

    // Navigation
    private readonly List<Employee> _employees = [];
    public IReadOnlyList<Employee> Employees => _employees.AsReadOnly();

    // ── Factory ──────────────────────────────────────────────────────────────

    public static Designation Create(string title, string? description = null, string? gradeLevel = null)
        => new()
        {
            Title       = title,
            Description = description,
            GradeLevel  = gradeLevel,
            CreatedAt   = DateTime.UtcNow,
            CreatedBy   = "system"
        };

    private Designation() { }
}
