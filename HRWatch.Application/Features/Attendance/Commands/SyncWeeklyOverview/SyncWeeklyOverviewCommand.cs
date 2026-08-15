using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Attendance.Commands.SyncWeeklyOverview;

public record SyncWeeklyOverviewCommand(
    string TriggeredBy = "system"
) : ICommand<SyncWeeklyOverviewResult>;

public record SyncWeeklyOverviewResult(
    int TotalProcessed,
    int EmployeesSynced,
    int WeeklyRecordsSynced,
    DateTime SyncedAt);
