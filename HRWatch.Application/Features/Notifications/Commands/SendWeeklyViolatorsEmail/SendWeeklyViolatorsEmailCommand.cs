using System.Text;
using HRWatch.Application.Common;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Common;
using HRWatch.Domain.Entities;
using HRWatch.Domain.Enums;
using HRWatch.Domain.Services;
using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HRWatch.Application.Features.Notifications.Commands.SendWeeklyViolatorsEmail;

public record SendWeeklyViolatorsEmailResult(
    bool Success,
    int ViolatorsCount,
    int CriticalViolatorsCount,
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    List<string> RecipientsSentTo,
    string Message);

public record SendWeeklyViolatorsEmailCommand(
    DateOnly? CustomWeekStart = null,
    DateOnly? CustomWeekEnd = null,
    List<string>? CustomRecipients = null,
    string TriggeredBy = "Scheduler") : ICommand<Result<SendWeeklyViolatorsEmailResult>>;

public class SendWeeklyViolatorsEmailCommandHandler : ICommandHandler<SendWeeklyViolatorsEmailCommand, Result<SendWeeklyViolatorsEmailResult>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IWfoEvaluationService _wfoService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendWeeklyViolatorsEmailCommandHandler> _logger;

    public SendWeeklyViolatorsEmailCommandHandler(
        IApplicationDbContext dbContext,
        IWfoEvaluationService wfoService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<SendWeeklyViolatorsEmailCommandHandler> logger)
    {
        _dbContext = dbContext;
        _wfoService = wfoService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<SendWeeklyViolatorsEmailResult>> HandleAsync(SendWeeklyViolatorsEmailCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Calculate Target Completed Week (Monday to Sunday)
        var (weekStart, weekEnd) = GetTargetWeek(command.CustomWeekStart, command.CustomWeekEnd);
        var weekFriday = weekStart.AddDays(4);

        _logger.LogInformation("Generating Weekly Violators Email Report for completed week {WeekStart} to {WeekEnd} (Triggered by {TriggeredBy})...",
            weekStart, weekEnd, command.TriggeredBy);

        // 2. Fetch Active Policy & Rules
        var activePolicy = await _dbContext.Policies.FirstOrDefaultAsync(p => p.IsActive, cancellationToken);
        var rulesJson = activePolicy?.RulesJson;

        // 3. Fetch Active India Employees
        var employees = await _dbContext.Employees
            .Where(e => e.IsActive && e.Location == "India")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
        {
            _logger.LogWarning("No active India employees found for weekly violator report.");
            return Result<SendWeeklyViolatorsEmailResult>.Success(new SendWeeklyViolatorsEmailResult(
                true, 0, 0, weekStart, weekEnd, [], "No active employees found."));
        }

        var empIds = employees.Select(e => e.Id).ToList();

        // 4. Fetch Attendances for the target week (Monday through Sunday)
        var attendances = await _dbContext.DailyAttendances
            .Where(a => empIds.Contains(a.EmployeeId) && a.Date >= weekStart && a.Date <= weekEnd)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var attMap = attendances
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 5. Evaluate Compliance for Each Employee
        var violators = new List<(Employee Emp, int Required, int Present, int Absent, int Shortfall, ViolationSeverity? Severity)>();

        foreach (var emp in employees)
        {
            attMap.TryGetValue(emp.Id, out var empAtts);
            empAtts ??= [];

            int presentDays = empAtts.Count(a => a.Status == AttendanceStatus.P);
            int leaveDays = empAtts.Count(a => a.Status == AttendanceStatus.L);
            int wfhDays = empAtts.Count(a => a.Status == AttendanceStatus.W);
            int exceptionDays = empAtts.Count(a => a.Status == AttendanceStatus.E);
            int holidayDays = empAtts.Count(a => a.Status == AttendanceStatus.H);
            int absentDays = empAtts.Count(a => a.Status == AttendanceStatus.A);

            int requiredDays = _wfoService.GetRequiredWfoDays(emp.Designation, emp.IsDeployed, rulesJson);

            // Strict compliance check: Shortfall > 0 AND AbsentDays > 0
            var (isViolator, shortfall, severity) = _wfoService.EvaluateWeeklyCompliance(
                presentDays, requiredDays, leaveDays, wfhDays, exceptionDays, absentDays, holidayDays);

            if (isViolator && shortfall > 0 && absentDays > 0)
            {
                violators.Add((emp, requiredDays, presentDays, absentDays, shortfall, severity));
            }
        }

        // Sort by shortfall descending, then by name
        var sortedViolators = violators
            .OrderByDescending(v => v.Shortfall)
            .ThenBy(v => v.Emp.FullName)
            .ToList();

        int totalViolators = sortedViolators.Count;
        int criticalViolators = sortedViolators.Count(v => v.Shortfall >= 3 || v.Severity == ViolationSeverity.High);

        // 6. Resolve Recipients
        var recipients = command.CustomRecipients?.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();
        if (recipients == null || recipients.Count == 0)
        {
            var configRecipients = _configuration.GetSection("EmailSettings:HrRecipients")
                .GetChildren()
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .Distinct()
                .ToList();

            recipients = configRecipients.Count > 0 ? configRecipients : new List<string>();
        }

        if (recipients.Count == 0)
        {
            recipients.Add("spidyprabhakar@gmail.com");
            recipients.Add("prabhakar0tenn@gmail.com");
        }

        // 7. Generate Responsive Branded HTML Template
        string subject = $"[HRWatch Alert] Weekly WFO Violators Report: {weekStart:dd MMM} - {weekEnd:dd MMM yyyy} ({totalViolators} Violators)";
        string htmlBody = GenerateEmailHtml(weekStart, weekEnd, sortedViolators, totalViolators, criticalViolators);

        // 8. Dispatch Email
        bool emailSent = await _emailService.SendEmailAsync(recipients, subject, htmlBody, cancellationToken);

        _logger.LogInformation("Weekly Violators Email Report dispatch completed: Success={Success}, Violators={Count}, Recipients={Recipients}",
            emailSent, totalViolators, string.Join(", ", recipients));

        return Result<SendWeeklyViolatorsEmailResult>.Success(new SendWeeklyViolatorsEmailResult(
            emailSent,
            totalViolators,
            criticalViolators,
            weekStart,
            weekEnd,
            recipients,
            emailSent
                ? $"Weekly WFO Violators Report successfully sent to {string.Join(", ", recipients)}."
                : "Failed to dispatch email via SMTP. Check server logs for details."));
    }

    private static (DateOnly WeekStart, DateOnly WeekEnd) GetTargetWeek(DateOnly? customStart, DateOnly? customEnd)
    {
        if (customStart.HasValue && customEnd.HasValue)
        {
            return (customStart.Value, customEnd.Value);
        }

        var today = IndiaDateTime.Today;

        // Calculate Monday of current week
        int diff = (7 + ((int)today.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
        var thisMonday = today.AddDays(-diff);

        // If today is Sunday (e.g. Sunday 10 PM run), this week is the completed week!
        if (today.DayOfWeek == DayOfWeek.Sunday)
        {
            return (thisMonday, thisMonday.AddDays(6));
        }

        // Otherwise (Monday or mid-week), the last completed week is the previous week
        var prevMonday = thisMonday.AddDays(-7);
        return (prevMonday, prevMonday.AddDays(6));
    }

    private static string GenerateEmailHtml(
        DateOnly weekStart,
        DateOnly weekEnd,
        List<(Employee Emp, int Required, int Present, int Absent, int Shortfall, ViolationSeverity? Severity)> violators,
        int totalViolators,
        int criticalViolators)
    {
        var sb = new StringBuilder();

        sb.Append(@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Weekly WFO Attendance Compliance Report</title>
<style>
  body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 24px 0; }
  .container { max-width: 820px; margin: 0 auto; background-color: #ffffff; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(15, 23, 42, 0.05); }
  .header { background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); padding: 28px 32px; color: #ffffff; }
  .header h1 { margin: 0; font-size: 22px; font-weight: 700; letter-spacing: -0.5px; }
  .header p { margin: 6px 0 0 0; font-size: 13px; opacity: 0.95; }
  .content { padding: 28px 32px; }
  .stats-grid { display: table; width: 100%; margin-bottom: 24px; }
  .stat-card { display: table-cell; width: 33.33%; padding: 14px 16px; background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; text-align: left; }
  .stat-card:not(:last-child) { border-right: 8px solid transparent; }
  .stat-label { font-size: 11px; text-transform: uppercase; font-weight: 700; color: #64748b; letter-spacing: 0.5px; }
  .stat-value { font-size: 24px; font-weight: 700; color: #0f172a; margin-top: 4px; }
  .stat-value.red { color: #dc2626; }
  .stat-value.amber { color: #d97706; }
  .table-wrapper { border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; margin-top: 16px; }
  table { width: 100%; border-collapse: collapse; text-align: left; font-size: 12px; }
  thead { background-color: #f1f5f9; color: #475569; text-transform: uppercase; font-size: 11px; font-weight: 700; letter-spacing: 0.5px; }
  th, td { padding: 10px 14px; border-bottom: 1px solid #f1f5f9; vertical-align: middle; }
  tbody tr:nth-child(even) { background-color: #fafafa; }
  .badge { display: inline-block; padding: 2px 8px; border-radius: 12px; font-size: 11px; font-weight: 700; text-align: center; }
  .badge-high { background-color: #fee2e2; color: #991b1b; border: 1px solid #fecaca; }
  .badge-medium { background-color: #fef3c7; color: #92400e; border: 1px solid #fde68a; }
  .badge-low { background-color: #f1f5f9; color: #475569; border: 1px solid #e2e8f0; }
  .emp-code { font-family: monospace; font-size: 11px; color: #64748b; }
  .footer { background-color: #f8fafc; padding: 20px 32px; border-top: 1px solid #e2e8f0; text-align: center; font-size: 11px; color: #64748b; line-height: 1.5; }
  .btn-portal { display: inline-block; margin-top: 12px; padding: 8px 18px; background-color: #f59e0b; color: #ffffff; text-decoration: none; font-weight: 600; font-size: 12px; border-radius: 6px; }
</style>
</head>
<body>
<div class=""container"">
  <div class=""header"">
    <h1>CG Infinity &bull; HRWatch Compliance Alert</h1>
    <p>Weekly WFO Attendance Shortfall Report for Audit & Action</p>
  </div>
  
  <div class=""content"">
    <p style=""font-size: 13px; color: #334155; margin-top: 0; line-height: 1.5;"">
      Dear HR Team,<br>
      Below is the official attendance compliance report for the previous completed week: 
      <strong>");

        sb.Append(weekStart.ToString("dd MMM yyyy"));
        sb.Append(" &rarr; ");
        sb.Append(weekEnd.ToString("dd MMM yyyy"));
        sb.Append(@"</strong>.
    </p>

    <div class=""stats-grid"">
      <div class=""stat-card"">
        <div class=""stat-label"">Target Audit Week</div>
        <div class=""stat-value"" style=""font-size: 14px; margin-top: 8px;"">");
        sb.Append(weekStart.ToString("dd MMM"));
        sb.Append(" - ");
        sb.Append(weekEnd.ToString("dd MMM"));
        sb.Append(@"</div>
      </div>
      <div class=""stat-card"">
        <div class=""stat-label"">Total Violators</div>
        <div class=""stat-value amber"">");
        sb.Append(totalViolators);
        sb.Append(@"</div>
      </div>
      <div class=""stat-card"">
        <div class=""stat-label"">Critical Shortfalls (&ge;3d)</div>
        <div class=""stat-value red"">");
        sb.Append(criticalViolators);
        sb.Append(@"</div>
      </div>
    </div>

    <div class=""table-wrapper"">
      <table>
        <thead>
          <tr>
            <th>Employee</th>
            <th>Designation</th>
            <th style=""text-align: center;"">Req</th>
            <th style=""text-align: center;"">Present</th>
            <th style=""text-align: center;"">Absent</th>
            <th style=""text-align: center;"">Shortfall</th>
            <th style=""text-align: center;"">Severity</th>
          </tr>
        </thead>
        <tbody>");

        if (violators.Count == 0)
        {
            sb.Append(@"<tr><td colspan=""7"" style=""text-align: center; padding: 24px; color: #10b981; font-weight: 600;"">🎉 Outstanding! 100% Attendance Compliance for this week — Zero Violators detected.</td></tr>");
        }
        else
        {
            foreach (var v in violators)
            {
                string severityClass = v.Severity == ViolationSeverity.High ? "badge-high" :
                                       v.Severity == ViolationSeverity.Medium ? "badge-medium" : "badge-low";
                string severityLabel = v.Severity?.ToString() ?? (v.Shortfall >= 3 ? "High" : v.Shortfall >= 2 ? "Medium" : "Low");

                sb.Append("<tr>");
                sb.Append($"<td><strong>{v.Emp.FullName}</strong><br><span class=\"emp-code\">{v.Emp.EmployeeCode} &bull; {v.Emp.Email}</span></td>");
                sb.Append($"<td style=\"color: #475569;\">{v.Emp.Designation}</td>");
                sb.Append($"<td style=\"text-align: center; font-weight: 600;\">{v.Required}d</td>");
                sb.Append($"<td style=\"text-align: center; color: #16a34a; font-weight: 600;\">{v.Present}d</td>");
                sb.Append($"<td style=\"text-align: center; color: #dc2626; font-weight: 600;\">{v.Absent}d</td>");
                sb.Append($"<td style=\"text-align: center; color: #dc2626; font-weight: 700;\">{v.Shortfall}d</td>");
                sb.Append($"<td style=\"text-align: center;\"><span class=\"badge {severityClass}\">{severityLabel}</span></td>");
                sb.Append("</tr>");
            }
        }

        sb.Append(@"</tbody>
      </table>
    </div>
  </div>

  <div class=""footer"">
    <p>This is an automated system notification dispatched by HRWatch 2.0 Attendance Engine.<br>
    Please do not reply directly to this email.</p>
    <a href=""http://localhost:3000"" class=""btn-portal"">Open HRWatch 2.0 Portal &rarr;</a>
  </div>
</div>
</body>
</html>");

        return sb.ToString();
    }
}
