using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Reports.Queries.GetLatestWeeklyReport;

/// Query: Get the most recently generated weekly report
public record GetLatestWeeklyReportQuery : IQuery<WeeklyReportDto?> { }

//  DTOs
public record WeeklyReportDto
{
    public Guid     Id                    { get; init; }
    public string   Period                { get; init; } = string.Empty;
    public DateTime GeneratedAt           { get; init; }
    public int      TotalEmployees        { get; init; }
    public int      EmployeesWithViolations { get; init; }
    public decimal  ComplianceScore       { get; init; }
    public IReadOnlyList<WeeklyReportEntryDto> Entries { get; init; } = [];
}

public record WeeklyReportEntryDto
{
    public Guid    EmployeeId        { get; init; }
    public string  EmployeeName      { get; init; } = string.Empty;
    public string  Department        { get; init; } = string.Empty;
    public int     DaysPresent       { get; init; }
    public int     DaysAbsent        { get; init; }
    public int     DaysLate          { get; init; }
    public decimal TotalWorkHours    { get; init; }
    public int     ViolationCount    { get; init; }
    public decimal ComplianceScore   { get; init; }
}
