using HRWatch.Domain.Common;

namespace HRWatch.Domain.Entities;

/// <summary>
/// Represents a public holiday. The attendance engine uses this to mark days as Holiday
/// instead of Absent when calculating compliance.
/// </summary>
public class Holiday : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public DateTime Date { get; private set; }

    /// <summary>Which country/region this holiday applies to</summary>
    public string? Region { get; private set; }

    /// <summary>Whether this holiday applies to all departments or just specific ones</summary>
    public bool IsCompanyWide { get; private set; } = true;

    public string? ApplicableDepartment { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static Holiday Create(
        string name,
        DateTime date,
        string? region = null,
        bool isCompanyWide = true,
        string? applicableDepartment = null) => new()
    {
        Name                  = name,
        Date                  = date.Date,
        Region                = region,
        IsCompanyWide         = isCompanyWide,
        ApplicableDepartment  = applicableDepartment,
        CreatedAt             = DateTime.UtcNow,
        CreatedBy             = "system"
    };

    private Holiday() { }
}
