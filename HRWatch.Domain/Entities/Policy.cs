using HRWatch.Domain.Common;

namespace HRWatch.Domain.Entities;

/// <summary>
/// An HR policy defines the rules that govern employee behavior.
/// Examples: "Max late arrivals per month = 3", "Minimum daily work hours = 8"
/// 
/// Policies are evaluated by the PolicyEngine domain service.
/// </summary>
public class Policy : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Serialized JSON rules for flexibility.
    /// Example: { "maxLateArrivalsPerMonth": 3, "minDailyWorkHours": 8, "gracePeriodMinutes": 15 }
    /// This allows policies to be configured without code changes.
    /// </summary>
    public string RulesJson { get; private set; } = "{}";

    /// <summary>Which department this policy applies to. Null = applies to all.</summary>
    public string? ApplicableDepartment { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static Policy Create(
        string name,
        string description,
        string rulesJson,
        DateTime effectiveFrom,
        string? applicableDepartment = null,
        string createdBy = "system") => new()
    {
        Name                  = name,
        Description           = description,
        RulesJson             = rulesJson,
        EffectiveFrom         = effectiveFrom,
        ApplicableDepartment  = applicableDepartment,
        CreatedBy             = createdBy,
        CreatedAt             = DateTime.UtcNow
    };

    // ── Behavior ─────────────────────────────────────────────────────────────

    public void Deactivate(DateTime effectiveTo, string updatedBy)
    {
        IsActive    = false;
        EffectiveTo = effectiveTo;
        UpdatedAt   = DateTime.UtcNow;
        UpdatedBy   = updatedBy;
    }

    public void UpdateRules(string newRulesJson, string updatedBy)
    {
        RulesJson = newRulesJson;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    private Policy() { }
}
