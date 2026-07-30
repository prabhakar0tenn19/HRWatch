namespace HRWatch.Domain.ValueObjects;

/// <summary>
/// VALUE OBJECT: Encapsulates work hour calculations.
/// Handles edge cases like night shifts (check-out next day).
/// </summary>
public sealed class WorkHours
{
    public TimeSpan CheckIn  { get; }
    public TimeSpan CheckOut { get; }
    public decimal  TotalHours { get; }

    private WorkHours(TimeSpan checkIn, TimeSpan checkOut, decimal totalHours)
    {
        CheckIn    = checkIn;
        CheckOut   = checkOut;
        TotalHours = totalHours;
    }

    /// <summary>
    /// Calculates work hours, handling the case where someone works past midnight.
    /// </summary>
    public static WorkHours Calculate(TimeSpan checkIn, TimeSpan checkOut)
    {
        // Night shift: checkout is before checkin (e.g., 10pm to 2am next day)
        var duration = checkOut >= checkIn
            ? checkOut - checkIn
            : checkOut + TimeSpan.FromHours(24) - checkIn;

        var total = Math.Round((decimal)duration.TotalHours, 2);

        return new WorkHours(checkIn, checkOut, total);
    }

    /// <summary>Whether minimum daily hours were met based on a threshold</summary>
    public bool MeetsMinimum(decimal minimumHours) => TotalHours >= minimumHours;

    public override bool Equals(object? obj)
        => obj is WorkHours other && CheckIn == other.CheckIn && CheckOut == other.CheckOut;

    public override int GetHashCode() => HashCode.Combine(CheckIn, CheckOut);

    public override string ToString() => $"In: {CheckIn:hh\\:mm}, Out: {CheckOut:hh\\:mm}, Total: {TotalHours}h";
}
