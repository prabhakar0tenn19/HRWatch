namespace HRWatch.Domain.ValueObjects;

/// <summary>
/// VALUE OBJECT: Represents a range of dates (start and end, both inclusive).
/// 
/// Value Objects are different from Entities:
/// - Entities have identity (ID) — two employees with different IDs are different even if same name
/// - Value Objects have NO identity — two DateRanges covering same dates ARE equal
/// 
/// Value Objects are immutable — you can't change them, you create new ones.
/// This prevents subtle bugs where shared references get mutated unexpectedly.
/// </summary>
public sealed class DateRange
{
    public DateTime Start { get; }
    public DateTime End   { get; }

    public int TotalDays => (End - Start).Days + 1;

    public DateRange(DateTime start, DateTime end)
    {
        if (start > end)
            throw new ArgumentException("Start date must be before or equal to end date.");

        Start = start.Date;
        End   = end.Date;
    }

    /// <summary>Creates a DateRange for the current week (Monday to Sunday)</summary>
    public static DateRange CurrentWeek()
    {
        var today = DateTime.UtcNow.Date;
        var monday = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        // If today is Sunday, DayOfWeek = 0, so we go back 6 days
        if (today.DayOfWeek == DayOfWeek.Sunday) monday = today.AddDays(-6);
        return new DateRange(monday, monday.AddDays(6));
    }

    /// <summary>Creates a DateRange for the previous week</summary>
    public static DateRange PreviousWeek()
    {
        var current = CurrentWeek();
        return new DateRange(current.Start.AddDays(-7), current.End.AddDays(-7));
    }

    public bool Contains(DateTime date) => date.Date >= Start && date.Date <= End;

    public bool Overlaps(DateRange other) => Start <= other.End && End >= other.Start;

    // Value equality — two DateRanges are equal if they cover the same period
    public override bool Equals(object? obj)
        => obj is DateRange other && Start == other.Start && End == other.End;

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public override string ToString() => $"{Start:yyyy-MM-dd} to {End:yyyy-MM-dd}";
}
