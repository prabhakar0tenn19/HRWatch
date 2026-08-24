namespace HRWatch.Infrastructure.Services.Email;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = "HRWatch - CG Infinity Attendance Engine";
    public string FromEmail { get; set; } = string.Empty;
    public List<string> HrRecipients { get; set; } = new();
}
