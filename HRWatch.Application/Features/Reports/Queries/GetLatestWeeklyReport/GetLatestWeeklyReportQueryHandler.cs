using HRWatch.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Reports.Queries.GetLatestWeeklyReport;

public class GetLatestWeeklyReportQueryHandler
    : IQueryHandler<GetLatestWeeklyReportQuery, WeeklyReportDto?>
{
    private readonly IWeeklyReportRepository _reportRepository;
    private readonly ILogger<GetLatestWeeklyReportQueryHandler> _logger;

    public GetLatestWeeklyReportQueryHandler(
        IWeeklyReportRepository reportRepository,
        ILogger<GetLatestWeeklyReportQueryHandler> logger)
    {
        _reportRepository = reportRepository;
        _logger           = logger;
    }

    public async Task<Result<WeeklyReportDto?>> HandleAsync(
        GetLatestWeeklyReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetLatestAsync(cancellationToken);

        if (report is null)
        {
            _logger.LogInformation("No weekly reports found in database.");
            return Result<WeeklyReportDto?>.Success(null);
        }

        var dto = new WeeklyReportDto
        {
            Id                      = report.Id,
            Period                  = report.Period.ToString(),
            GeneratedAt             = report.GeneratedAt,
            TotalEmployees          = report.TotalEmployees,
            EmployeesWithViolations = report.EmployeesWithViolations,
            ComplianceScore         = report.ComplianceScore,
            Entries                 = report.Entries.Select(e => new WeeklyReportEntryDto
            {
                EmployeeId      = e.EmployeeId,
                EmployeeName    = e.EmployeeFullName,
                Department      = e.EmployeeDepartment,
                DaysPresent     = e.DaysPresent,
                DaysAbsent      = e.DaysAbsent,
                DaysLate        = e.DaysLate,
                TotalWorkHours  = e.TotalWorkHours,
                ViolationCount  = e.ViolationCount,
                ComplianceScore = e.ComplianceScore
            }).ToList()
        };

        return Result<WeeklyReportDto?>.Success(dto);
    }
}
