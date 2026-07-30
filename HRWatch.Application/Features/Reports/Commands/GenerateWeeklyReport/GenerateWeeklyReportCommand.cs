using HRWatch.Application.Common.Abstractions;
using HRWatch.Domain.ValueObjects;

namespace HRWatch.Application.Features.Reports.Commands.GenerateWeeklyReport;


public record GenerateWeeklyReportCommand : ICommand<Guid>
{
  
    public DateTime WeekStartDate { get; init; } = DateRange.PreviousWeek().Start;

    public string TriggeredBy { get; init; } = "system";
}
