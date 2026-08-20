namespace HRWatch.Domain.Common;

public static class IndiaDateTime
{
    private static readonly TimeZoneInfo IstZone = FindIstTimeZone();

    private static TimeZoneInfo FindIstTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); // Windows
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); // Linux / Docker / Azure Linux
            }
            catch
            {
                // Fallback fixed +5:30 offset
                return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromHours(5.5), "India Standard Time", "IST");
            }
        }
    }

    public static TimeZoneInfo TimeZone => IstZone;
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
    public static DateOnly Today => DateOnly.FromDateTime(Now);
}
