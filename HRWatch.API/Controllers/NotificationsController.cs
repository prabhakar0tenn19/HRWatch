using HRWatch.Application.Common.Interfaces;
using HRWatch.Application.Features.Notifications.Commands.SendWeeklyViolatorsEmail;
using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HRWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ICommandMediator _commandMediator;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        ICommandMediator commandMediator,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<NotificationsController> logger)
    {
        _commandMediator = commandMediator;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Sends a test email to verify Gmail SMTP configuration and connectivity.
    /// </summary>
    [HttpPost("test-email")]
    public async Task<IActionResult> SendTestEmail([FromQuery] string? recipientEmail, CancellationToken cancellationToken)
    {
        var recipients = new List<string>();
        if (!string.IsNullOrWhiteSpace(recipientEmail))
        {
            recipients.Add(recipientEmail.Trim());
        }
        else
        {
            var configRecipients = _configuration.GetSection("EmailSettings:HrRecipients").Get<List<string>>();
            if (configRecipients != null && configRecipients.Count > 0)
            {
                recipients.AddRange(configRecipients);
            }
            else
            {
                recipients.Add("spidyprabhakar@gmail.com");
                recipients.Add("prabhakar0tenn@gmail.com");
            }
        }

        string subject = "[HRWatch 2.0] SMTP Test Email — Connection Verified";
        string htmlBody = $@"
<!DOCTYPE html>
<html>
<body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: #f8fafc; padding: 24px;"">
  <div style=""max-width: 600px; margin: 0 auto; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 12px; padding: 32px; box-shadow: 0 4px 12px rgba(15,23,42,0.05);"">
    <div style=""background: #f59e0b; color: #ffffff; padding: 16px 20px; border-radius: 8px; font-size: 18px; font-weight: bold; margin-bottom: 20px;"">
      CG Infinity &bull; HRWatch 2.0 SMTP Verified
    </div>
    <p style=""font-size: 14px; color: #334155; line-height: 1.6;"">
      Hello HR Team,<br><br>
      This is a <strong>successful test email</strong> from the HRWatch 2.0 backend system. Your Gmail SMTP integration (Host: <code>smtp.gmail.com:587</code>) is active and properly authenticated.
    </p>
    <div style=""background: #f1f5f9; padding: 14px; border-radius: 6px; font-size: 12px; color: #475569; margin: 20px 0;"">
      <strong>Timestamp (IST):</strong> {HRWatch.Domain.Common.IndiaDateTime.Now:dd MMM yyyy, hh:mm:ss tt}<br>
      <strong>Automated Schedule:</strong> Every Sunday at 10:00 PM IST (22:00)
    </div>
    <p style=""font-size: 12px; color: #64748b;"">
      Regards,<br>
      <strong>HRWatch Automated Attendance System</strong>
    </p>
  </div>
</body>
</html>";

        _logger.LogInformation("Sending manual test email to {Recipients}...", string.Join(", ", recipients));
        bool success = await _emailService.SendEmailAsync(recipients, subject, htmlBody, cancellationToken);

        if (success)
        {
            return Ok(new
            {
                success = true,
                message = $"Test email successfully delivered to {string.Join(", ", recipients)}.",
                recipients,
                sentAt = DateTime.UtcNow
            });
        }

        return StatusCode(500, new
        {
            success = false,
            message = "Failed to send test email via SMTP. Check server logs for details.",
            recipients
        });
    }

    /// <summary>
    /// Manually triggers the weekly violators email dispatch report (for immediate testing or audit).
    /// </summary>
    [HttpPost("send-weekly-violators")]
    public async Task<IActionResult> SendWeeklyViolators(
        [FromQuery] DateOnly? customWeekStart,
        [FromQuery] DateOnly? customWeekEnd,
        [FromQuery] string? recipientEmail,
        CancellationToken cancellationToken)
    {
        List<string>? customRecipients = !string.IsNullOrWhiteSpace(recipientEmail)
            ? new List<string> { recipientEmail.Trim() }
            : null;

        var command = new SendWeeklyViolatorsEmailCommand(
            CustomWeekStart: customWeekStart,
            CustomWeekEnd: customWeekEnd,
            CustomRecipients: customRecipients,
            TriggeredBy: "ManualApiTrigger");

        var result = await _commandMediator.SendAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new { error = result.ErrorMessage });
    }
}
