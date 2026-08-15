using System.Text.Json;
using HRWatch.Domain.Entities;
using HRWatch.Domain.Enums;

namespace HRWatch.Domain.Services;

public class ViolationCalculationService : IViolationCalculationService
{
    public Violation? CalculateWfoViolation(Employee employee, WeeklyAttendance attendance, Policy policy)
    {
        if (employee is null || attendance is null || policy is null)
            return null;

        int requiredDays = 5;

        if (!string.IsNullOrWhiteSpace(policy.RulesJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(policy.RulesJson);
                if (doc.RootElement.TryGetProperty("MinWfoDaysPerWeek", out var prop) && prop.TryGetInt32(out var minDays))
                {
                    requiredDays = minDays;
                }
            }
            catch
            {
                requiredDays = GetRequiredDaysByDesignation(employee.Designation?.Title);
            }
        }
        else
        {
            requiredDays = GetRequiredDaysByDesignation(employee.Designation?.Title);
        }

        int actualPresent = attendance.PresentCount;
        if (actualPresent >= requiredDays)
        {
            return null;
        }

        int shortfall = requiredDays - actualPresent;
        var severity = shortfall switch
        {
            1 => ViolationSeverity.Low,
            2 => ViolationSeverity.Medium,
            _ => ViolationSeverity.High
        };

        var details = $"WFO Shortfall: Required {requiredDays} days, Actual {actualPresent} days (Shortfall: {shortfall} days). Designation: {employee.Designation?.Title ?? "N/A"}";

        return Violation.Create(
            employeeId: employee.Id,
            type: ViolationType.UnauthorizedAbsence,
            severity: severity,
            description: details,
            occurredOn: attendance.WeekStartDate,
            policyId: policy.Id,
            createdBy: "ViolationEngine");
    }

    private static int GetRequiredDaysByDesignation(string? designation)
    {
        if (string.IsNullOrWhiteSpace(designation)) return 5;

        var title = designation.Trim().ToUpperInvariant();
        if (title is "SDE" or "C1" or "C2") return 5;
        if (title.StartsWith("A") || title.StartsWith("M") || title.StartsWith("P")) return 3;

        return 5;
    }
}
