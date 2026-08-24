using System.Net;
using System.Net.Mail;
using HRWatch.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRWatch.Infrastructure.Services.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(IEnumerable<string> toRecipients, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var recipients = toRecipients?.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();
        if (recipients == null || recipients.Count == 0)
        {
            _logger.LogWarning("No valid recipients provided for email subject '{Subject}'", subject);
            return false;
        }

        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000
            };

            var senderEmail = !string.IsNullOrWhiteSpace(_settings.FromEmail) ? _settings.FromEmail : _settings.Username;
            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            foreach (var recipient in recipients)
            {
                message.To.Add(recipient.Trim());
            }

            _logger.LogInformation("Dispatching SMTP email '{Subject}' to {Count} recipients ({Recipients})...",
                subject, recipients.Count, string.Join(", ", recipients));

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("SMTP email '{Subject}' sent successfully!", subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch SMTP email '{Subject}' via host '{Host}:{Port}'",
                subject, _settings.Host, _settings.Port);
            return false;
        }
    }
}
