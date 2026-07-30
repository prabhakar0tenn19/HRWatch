using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Attendance.Commands.SyncAttendance;


public record SyncAttendanceCommand : ICommand<SyncAttendanceResult>
{
    public DateTime FromDate    { get; init; } = DateTime.UtcNow.Date;
    public DateTime ToDate      { get; init; } = DateTime.UtcNow.Date;
    public string   TriggeredBy { get; init; } = "system";

  
    public Guid? EmployeeId { get; init; }
}

public record SyncAttendanceResult(
    int RecordsSynced,
    int RecordsCreated,
    int RecordsUpdated,
    DateTime SyncedAt);
