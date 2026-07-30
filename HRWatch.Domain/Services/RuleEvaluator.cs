using HRWatch.Domain.Entities;
using HRWatch.Domain.Enums;

namespace HRWatch.Domain.Services;

/// <summary>
/// DOMAIN SERVICE: RuleEvaluator
/// 
/// A lower-level service used by PolicyEngine and ComplianceEvaluator.
/// Provides reusable rule-checking predicates so the same logic isn't duplicated.
/// 
/// Think of this as the "math engine" — it checks individual conditions.
/// PolicyEngine and ComplianceEvaluator use it to build higher-level logic.
/// </summary>
public class RuleEvaluator
{
    /// <summary>Checks if an employee arrived late (after grace period)</summary>
    public bool IsLateArrival(Attendance attendance, TimeSpan expectedStartTime, int gracePeriodMinutes)
    {
        if (attendance.CheckIn is null) return false;
        var latestAllowedStart = expectedStartTime.Add(TimeSpan.FromMinutes(gracePeriodMinutes));
        return attendance.CheckIn > latestAllowedStart;
    }

    /// <summary>Checks if an employee left before required end time</summary>
    public bool IsEarlyDeparture(Attendance attendance, TimeSpan expectedEndTime)
    {
        if (attendance.CheckOut is null) return false;
        return attendance.CheckOut < expectedEndTime;
    }

    /// <summary>Checks if an employee met the minimum daily work hours</summary>
    public bool MetMinimumHours(Attendance attendance, decimal minimumHours)
    {
        return attendance.TotalWorkHours.HasValue && attendance.TotalWorkHours >= minimumHours;
    }

    /// <summary>
    /// Returns the number of working days in a date range,
    /// excluding weekends and the provided holidays.
    /// </summary>
    public int CountWorkingDays(DateTime start, DateTime end, IReadOnlyList<Holiday> holidays)
    {
        var holidayDates = holidays.Select(h => h.Date.Date).ToHashSet();
        var count = 0;
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (holidayDates.Contains(date)) continue;
            count++;
        }
        return count;
    }

    /// <summary>Checks if an employee had unauthorized absence (absent without leave approval)</summary>
    public bool IsUnauthorizedAbsence(Attendance attendance)
        => attendance.Status == AttendanceStatus.Absent;

    /// <summary>
    /// Computes attendance summary stats for an employee over a period.
    /// Reusable across weekly reports and compliance checks.
    /// </summary>
    public AttendanceSummary ComputeSummary(IReadOnlyList<Attendance> records)
    {
        return new AttendanceSummary(
            DaysPresent : records.Count(a => a.Status == AttendanceStatus.Present),
            DaysAbsent  : records.Count(a => a.Status == AttendanceStatus.Absent),
            DaysLate    : records.Count(a => a.Status == AttendanceStatus.Late),
            DaysOnLeave : records.Count(a => a.Status == AttendanceStatus.OnLeave),
            TotalHours  : records.Sum(a => a.TotalWorkHours ?? 0));
    }
}

/// <summary>
/// Plain summary of attendance data — returned by RuleEvaluator, used by ComplianceEvaluator.
/// </summary>
public record AttendanceSummary(
    int     DaysPresent,
    int     DaysAbsent,
    int     DaysLate,
    int     DaysOnLeave,
    decimal TotalHours);
