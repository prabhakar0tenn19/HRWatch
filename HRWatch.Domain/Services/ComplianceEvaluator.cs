using HRWatch.Domain.Entities;

namespace HRWatch.Domain.Services;

/// <summary>
/// DOMAIN SERVICE: ComplianceEvaluator
/// 
/// Calculates a compliance score (0-100) for an employee over a given period.
/// Score is used to generate WeeklyReport entries.
/// 
/// Score formula (can be tuned):
///   Base: 100
///   -10 per absence (max -30)
///   -5 per late arrival (max -20)
///   -3 per insufficient hours day (max -15)
///   -2 per other violation (max -10)
/// </summary>
public class ComplianceEvaluator
{
    private const decimal BaseScore            = 100m;
    private const decimal AbsencePenalty       = 10m;
    private const decimal LatePenalty          = 5m;
    private const decimal ShortHoursPenalty    = 3m;
    private const decimal OtherViolationPenalty = 2m;

    private const decimal MaxAbsencePenalty    = 30m;
    private const decimal MaxLatePenalty       = 20m;
    private const decimal MaxShortHoursPenalty = 15m;
    private const decimal MaxOtherPenalty      = 10m;

    /// <summary>
    /// Calculates the compliance score for a single employee for a given week.
    /// Returns a score between 0 and 100.
    /// </summary>
    public ComplianceResult Evaluate(
        Employee employee,
        IReadOnlyList<Attendance> weekAttendance,
        IReadOnlyList<Violation>  weekViolations)
    {
        var absences     = weekAttendance.Count(a => a.Status == Enums.AttendanceStatus.Absent);
        var lateArrivals = weekAttendance.Count(a => a.Status == Enums.AttendanceStatus.Late);
        var shortDays    = weekViolations.Count(v => v.Type == Enums.ViolationType.InsufficientWorkHours);
        var otherViolations = weekViolations.Count(v =>
            v.Type != Enums.ViolationType.InsufficientWorkHours &&
            v.Type != Enums.ViolationType.ExcessiveLateArrivals &&
            v.Type != Enums.ViolationType.ExcessiveAbsences);

        var score = BaseScore;
        score -= Math.Min(absences * AbsencePenalty, MaxAbsencePenalty);
        score -= Math.Min(lateArrivals * LatePenalty, MaxLatePenalty);
        score -= Math.Min(shortDays * ShortHoursPenalty, MaxShortHoursPenalty);
        score -= Math.Min(otherViolations * OtherViolationPenalty, MaxOtherPenalty);

        // Score cannot go below 0
        score = Math.Max(0, score);

        return new ComplianceResult(
            EmployeeId     : employee.Id,
            EmployeeName   : employee.FullName,
            Score          : Math.Round(score, 2),
            Absences       : absences,
            LateArrivals   : lateArrivals,
            ViolationCount : weekViolations.Count);
    }
}

/// <summary>
/// Result of compliance evaluation — a pure data transfer object from the domain service.
/// </summary>
public record ComplianceResult(
    Guid    EmployeeId,
    string  EmployeeName,
    decimal Score,
    int     Absences,
    int     LateArrivals,
    int     ViolationCount);
