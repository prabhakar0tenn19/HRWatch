using HRWatch.Domain.Common;

namespace HRWatch.Domain.Entities;

public class Policy : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string RulesJson { get; private set; } = "{}";

    public Guid? DesignationId { get; private set; }
    public Designation? Designation { get; private set; }

    public string? ApplicableDepartment { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }

    private Policy() { }

    public static Policy Create(
        string name,
        string description,
        string rulesJson,
        DateTime effectiveFrom,
        Guid? designationId = null,
        string? applicableDepartment = null,
        DateTime? effectiveTo = null,
        string createdBy = "system")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Policy name is required.", nameof(name));

        return new Policy
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            RulesJson = rulesJson,
            EffectiveFrom = effectiveFrom.Date,
            EffectiveTo = effectiveTo?.Date,
            DesignationId = designationId,
            ApplicableDepartment = applicableDepartment,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate(DateTime effectiveTo, string updatedBy)
    {
        IsActive = false;
        EffectiveTo = effectiveTo.Date;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void UpdateRules(string newRulesJson, string updatedBy)
    {
        RulesJson = newRulesJson;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
