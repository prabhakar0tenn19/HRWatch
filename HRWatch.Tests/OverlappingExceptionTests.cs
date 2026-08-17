using Xunit;

namespace HRWatch.Tests;

public class OverlappingExceptionTests
{
    private static bool CheckOverlap(DateOnly existingFrom, DateOnly existingTo, DateOnly newFrom, DateOnly newTo)
    {
        return existingFrom <= newTo && existingTo >= newFrom;
    }

    [Theory]
    // Exact match
    [InlineData("2026-08-10", "2026-08-15", "2026-08-10", "2026-08-15", true)]
    // Inside existing range
    [InlineData("2026-08-10", "2026-08-20", "2026-08-12", "2026-08-15", true)]
    // Overlapping end
    [InlineData("2026-08-10", "2026-08-15", "2026-08-14", "2026-08-20", true)]
    // Overlapping start
    [InlineData("2026-08-10", "2026-08-15", "2026-08-05", "2026-08-11", true)]
    // Completely non-overlapping (Before)
    [InlineData("2026-08-10", "2026-08-15", "2026-08-01", "2026-08-08", false)]
    // Completely non-overlapping (After)
    [InlineData("2026-08-10", "2026-08-15", "2026-08-16", "2026-08-20", false)]
    public void OverlapLogic_EvaluatesDateRangesCorrectly(
        string exFrom, string exTo, string newFrom, string newTo, bool expectedOverlap)
    {
        var eFrom = DateOnly.Parse(exFrom);
        var eTo = DateOnly.Parse(exTo);
        var nFrom = DateOnly.Parse(newFrom);
        var nTo = DateOnly.Parse(newTo);

        var result = CheckOverlap(eFrom, eTo, nFrom, nTo);
        Assert.Equal(expectedOverlap, result);
    }
}
