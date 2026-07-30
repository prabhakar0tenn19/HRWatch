namespace HRWatch.Infrastructure.Auth;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string SecretKey { get; init; } = "HRWatchSuperSecretKeyThatIsAtLeast32BytesLong!";
    public string Issuer { get; init; } = "HRWatchAPI";
    public string Audience { get; init; } = "HRWatchApp";
    public int ExpiryMinutes { get; init; } = 120;
}
