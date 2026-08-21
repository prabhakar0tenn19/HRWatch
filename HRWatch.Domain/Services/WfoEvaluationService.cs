using System.Text.Json;
using HRWatch.Domain.Enums;

namespace HRWatch.Domain.Services;

public class WfoEvaluationService : IWfoEvaluationService
{
    public int GetRequiredWfoDays(string? designation, bool isDeployed, string? rulesJson = null)
    {
        // 1. Bench employees (isDeployed == false) ALWAYS require 5 days WFO
        if (!isDeployed)
        {
            return 5;
        }

        // 2. Check dynamic rulesJson if provided
        if (!string.IsNullOrWhiteSpace(rulesJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(rulesJson);
                if (doc.RootElement.TryGetProperty("MinWfoDaysPerWeek", out var minDaysObj))
                {
                    if (designation != null)
                    {
                        var titleUpper = designation.Trim().ToUpperInvariant();
                        foreach (var prop in minDaysObj.EnumerateObject())
                        {
                            if (titleUpper.Contains(prop.Name.ToUpperInvariant()))
                            {
                                return prop.Value.GetInt32();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback to standard policy rules
            }
        }

        // 3. Standard Company Policy fallback (policy.md)
        if (string.IsNullOrWhiteSpace(designation))
        {
            return 5;
        }

        var desig = designation.Trim().ToUpperInvariant();

        // SDE, Consultant 1/2, Interns -> 5 days
        if (desig is "SDE" or "C1" or "C2" or "INTERN" || 
            desig.StartsWith("CONSULTANT") || 
            desig.StartsWith("SOFTWARE DEV"))
        {
            return 5;
        }

        // Associate 1/2, Manager 1/2/3, Principal 1/2/3 -> 3 days
        if (desig is "A1" or "A2" or "M1" or "M2" or "M3" or "P1" or "P2" ||
            desig.StartsWith("ASSOCIATE") ||
            desig.StartsWith("MANAGER") ||
            desig.StartsWith("PRINCIPAL") ||
            desig.StartsWith("DIRECTOR") ||
            desig.StartsWith("VICE PRESIDENT") ||
            desig is "CHAIRMAN")
        {
            return 3;
        }

        // Default safe fallback
        return 5;
    }

    public (bool IsViolator, int Shortfall, ViolationSeverity? Severity) EvaluateWeeklyCompliance(
        int actualPresentDays, 
        int requiredDays, 
        int approvedLeaveDays = 0, 
        int approvedWfhDays = 0, 
        int exceptionDays = 0, 
        int absentDays = 0,
        int holidayDays = 0)
    {
        // If there are no unauthorized absences (A), the employee is NOT a violator!
        if (absentDays <= 0)
        {
            return (false, 0, null);
        }

        // Shortfall is exactly the count of unauthorized absent days
        int shortfall = absentDays;

        var severity = shortfall switch
        {
            1 => ViolationSeverity.Low,
            2 => ViolationSeverity.Medium,
            _ => ViolationSeverity.High
        };

        return (true, shortfall, severity);
    }
}
