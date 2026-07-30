using HRWatch.Domain.Entities;
using HRWatch.Domain.Enums;

namespace HRWatch.Domain.Services;


public class RuleEvaluator
{
      public bool IsLateArrival(Attendance attendance, TimeSpan expectedStartTime, int gracePeriodMinutes)
    {
        if (attendance.CheckIn is null) return false;
        var latestAllowedStart = expectedStartTime.Add(TimeSpan.FromMinutes(gracePeriodMinutes));
        return attendance.CheckIn > latestAllowedStart;
    }

     public bool IsEarlyDeparture(Attendance attendance, TimeSpan expectedEndTime)
    {
        if (attendance.CheckOut is null) return false;
        return attendance.CheckOut < expectedEndTime;
    }

       public bool MetMinimumHours(Attendance attendance, decimal minimumHours)
    {
        return attendance.TotalWorkHours.HasValue && attendance.TotalWorkHours >= minimumHours;
    }

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

       public bool IsUnauthorizedAbsence(Attendance attendance)
        => attendance.Status == AttendanceStatus.Absent;

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


public record AttendanceSummary(
    int     DaysPresent,
    int     DaysAbsent,
    int     DaysLate,
    int     DaysOnLeave,
    decimal TotalHours);
