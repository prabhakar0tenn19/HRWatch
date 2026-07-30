using HRWatch.Domain.Common;
using HRWatch.Domain.ValueObjects;

namespace HRWatch.Domain.Entities;

/// <summary>
/// A weekly report is generated every week by the GenerateWeeklyReportJob.
/// It summarizes attendance, violations, and compliance for all employees for that week.
/// </summary>
public class WeeklyReport : AuditableEntity
{
    /// <summary>The date range this report covers (Monday to Sunday)</summary>
    public DateRange Period { get; private set; } = null!;

    public DateTime GeneratedAt { get; private set; }

    /// <summary>Total employees in the system at time of report generation</summary>
    public int TotalEmployees { get; private set; }

    /// <summary>Number of employees with at least one violation this week</summary>
    public int EmployeesWithViolations { get; private set; }

    /// <summary>Overall compliance score (0-100)</summary>
    public decimal ComplianceScore { get; private set; }

    public string? Notes { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    private readonly List<WeeklyReportEntry> _entries = [];
    public IReadOnlyList<WeeklyReportEntry> Entries => _entries.AsReadOnly();

    // ── Factory ──────────────────────────────────────────────────────────────

    public static WeeklyReport Create(
        DateRange period,
        int totalEmployees,
        string createdBy = "system") => new()
    {
        Period            = period,
        GeneratedAt       = DateTime.UtcNow,
        TotalEmployees    = totalEmployees,
        CreatedBy         = createdBy,
        CreatedAt         = DateTime.UtcNow
    };

    // ── Behavior ─────────────────────────────────────────────────────────────

    public void AddEntry(WeeklyReportEntry entry)
        => _entries.Add(entry);

    public void FinalizeReport()
    {
        EmployeesWithViolations = _entries.Count(e => e.ViolationCount > 0);
        ComplianceScore         = _entries.Any()
            ? Math.Round(_entries.Average(e => e.ComplianceScore), 2)
            : 100m;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddNotes(string notes) => Notes = notes;

    private WeeklyReport() { }
}
