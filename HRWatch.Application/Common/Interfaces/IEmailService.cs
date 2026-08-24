namespace HRWatch.Application.Common.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(IEnumerable<string> toRecipients, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
