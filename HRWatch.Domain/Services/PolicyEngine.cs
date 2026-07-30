using HRWatch.Domain.Entities;
using HRWatch.Domain.Enums;

namespace HRWatch.Domain.Services;

/// <summary>
/// DOMAIN SERVICE: PolicyEngine
/// 
/// A Domain Service is used when business logic:
/// 1. Doesn't naturally belong to a single entity
/// 2. Involves multiple entities working together
/// 3. Has complex rules that would bloat an entity
/// 
/// PolicyEngine evaluates whether a set of attendance records
/// violates the rules defined in a Policy.
/// 
/// NOTE: This is pure domain logic — no EF Core, no HTTP, no DI containers.
/// It takes plain objects and returns plain objects.
/// </summary>
public class PolicyEngine
{
    /// <summary>
    /// Evaluates all attendance records for an employee against the applicable policies.
    /// Returns a list of violations found.
    /// </summary>
    public IReadOnlyList<Violation> EvaluateAttendance(
        Employee employee,
        IReadOnlyList<Attendance> attendanceRecords,
        IReadOnlyList<Policy> policies,
        DateTime forPeriodStart,
        DateTime forPeriodEnd)
    {
        var violations = new List<Violation>();

        foreach (var policy in policies.Where(p => p.IsActive))
        {
            // Parse the JSON rules into a domain-friendly object
            var rules = PolicyRules.ParseFrom(policy.RulesJson);

            // ── Rule 1: Max Late Arrivals ──────────────────────────────────
            if (rules.MaxLateArrivalsPerMonth > 0)
            {
                var lateCount = attendanceRecords
                    .Count(a => a.Status == AttendanceStatus.Late
                             && a.Date >= forPeriodStart
                             && a.Date <= forPeriodEnd);

                if (lateCount > rules.MaxLateArrivalsPerMonth)
                {
                    violations.Add(Violation.Create(
                        employee.Id,
                        ViolationType.ExcessiveLateArrivals,
                        $"Employee arrived late {lateCount} times. Policy allows max {rules.MaxLateArrivalsPerMonth}.",
                        forPeriodEnd,
                        policy.Id));
                }
            }

            // ── Rule 2: Max Absences ────────────────────────────────────────
            if (rules.MaxAbsencesPerMonth > 0)
            {
                var absentCount = attendanceRecords
                    .Count(a => a.Status == AttendanceStatus.Absent
                             && a.Date >= forPeriodStart
                             && a.Date <= forPeriodEnd);

                if (absentCount > rules.MaxAbsencesPerMonth)
                {
                    violations.Add(Violation.Create(
                        employee.Id,
                        ViolationType.ExcessiveAbsences,
                        $"Employee was absent {absentCount} times. Policy allows max {rules.MaxAbsencesPerMonth}.",
                        forPeriodEnd,
                        policy.Id));
                }
            }

            // ── Rule 3: Minimum Daily Work Hours ────────────────────────────
            if (rules.MinDailyWorkHours > 0)
            {
                var shortDays = attendanceRecords
                    .Where(a => a.Status == AttendanceStatus.Present
                             && a.TotalWorkHours.HasValue
                             && a.TotalWorkHours < rules.MinDailyWorkHours
                             && a.Date >= forPeriodStart
                             && a.Date <= forPeriodEnd)
                    .ToList();

                foreach (var shortDay in shortDays)
                {
                    violations.Add(Violation.Create(
                        employee.Id,
                        ViolationType.InsufficientWorkHours,
                        $"Worked only {shortDay.TotalWorkHours}h on {shortDay.Date:yyyy-MM-dd}. Minimum is {rules.MinDailyWorkHours}h.",
                        shortDay.Date,
                        policy.Id));
                }
            }
        }

        return violations.AsReadOnly();
    }
}

/// <summary>
/// Internal helper: parsed version of a policy's JSON rules.
/// This keeps the Policy entity clean (just stores JSON string).
/// </summary>
internal class PolicyRules
{
    public int     MaxLateArrivalsPerMonth { get; init; }
    public int     MaxAbsencesPerMonth     { get; init; }
    public decimal MinDailyWorkHours       { get; init; }
    public int     GracePeriodMinutes      { get; init; }

    public static PolicyRules ParseFrom(string rulesJson)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<PolicyRules>(rulesJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new PolicyRules();
        }
        catch
        {
            // Malformed JSON — return defaults (no restrictions)
            return new PolicyRules();
        }
    }
}
