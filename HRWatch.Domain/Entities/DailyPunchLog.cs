using HRWatch.Domain.Common;

namespace HRWatch.Domain.Entities;

public class DailyPunchLog : BaseEntity
{
    public string EmployeeCode { get; set; } = string.Empty; // "CGI705"
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly PunchDate { get; set; }
    public TimeOnly PunchTime { get; set; }
    public string DeviceName { get; set; } = string.Empty; // "Main Gate Entry"
    public int EntryExitType { get; set; } = 1; // 1 = Entry, 0 = Exit
    public string? RawLogIndex { get; set; } // IndexNo from COSEC
}
