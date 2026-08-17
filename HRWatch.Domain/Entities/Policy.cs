using HRWatch.Domain.Common;

namespace HRWatch.Domain.Entities;

public class Policy : BaseEntity
{
    public int Version { get; set; } = 1;
    public string PolicyName { get; set; } = "CG India WFO Policy";
    public string RulesJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string CreatedBy { get; set; } = "System";

    // Navigation collection
    public ICollection<DailyAttendance> Attendances { get; set; } = new List<DailyAttendance>();
}
