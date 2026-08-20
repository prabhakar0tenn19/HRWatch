using HRWatch.Domain.Enums;
using HRWatch.Domain.Services;
using Xunit;

namespace HRWatch.Tests;

public class WfoEvaluationServiceTests
{
    private readonly IWfoEvaluationService _sut = new WfoEvaluationService();

    [Theory]
    [InlineData("SDE", true, 5)]
    [InlineData("Software Developer", true, 5)]
    [InlineData("Consultant 1", true, 5)]
    [InlineData("Consultant 2", true, 5)]
    [InlineData("Intern", true, 5)]
    [InlineData("Associate 1", true, 3)]
    [InlineData("Associate 2", true, 3)]
    [InlineData("Manager 1", true, 3)]
    [InlineData("Manager 2", true, 3)]
    [InlineData("Principal", true, 3)]
    [InlineData("Director", true, 3)]
    public void GetRequiredWfoDays_DeployedEmployees_ReturnsExpectedDays(string designation, bool isDeployed, int expectedDays)
    {
        var result = _sut.GetRequiredWfoDays(designation, isDeployed);
        Assert.Equal(expectedDays, result);
    }

    [Theory]
    [InlineData("Manager 1", false, 5)] // Bench manager must still do 5 days
    [InlineData("Associate 2", false, 5)] // Bench associate must still do 5 days
    [InlineData("SDE", false, 5)]
    public void GetRequiredWfoDays_BenchEmployees_AlwaysReturnsFiveDays(string designation, bool isDeployed, int expectedDays)
    {
        var result = _sut.GetRequiredWfoDays(designation, isDeployed);
        Assert.Equal(expectedDays, result);
    }

    [Theory]
    [InlineData(5, 5, 0, 0, 0, 0, false, 0, null)]
    [InlineData(4, 5, 0, 0, 0, 1, true, 1, ViolationSeverity.Low)]
    [InlineData(3, 5, 0, 0, 0, 2, true, 2, ViolationSeverity.Medium)]
    [InlineData(2, 5, 0, 0, 0, 3, true, 3, ViolationSeverity.High)]
    [InlineData(0, 5, 0, 0, 0, 5, true, 5, ViolationSeverity.High)]
    [InlineData(3, 3, 0, 2, 0, 0, false, 0, null)] // Manager 3 P + 2 WFH + 0 A = Compliant
    [InlineData(2, 3, 0, 2, 0, 1, true, 1, ViolationSeverity.Low)] // Manager 2 P + 2 WFH + 1 A = Violator (Low)
    [InlineData(3, 5, 2, 0, 0, 0, false, 0, null)] // SDE 3 P + 2 Approved Leaves + 0 A = Compliant!
    [InlineData(2, 5, 2, 0, 0, 1, true, 1, ViolationSeverity.Low)] // SDE 2 P + 2 Leaves + 1 A = Violator (1 shortfall)
    [InlineData(0, 5, 5, 0, 0, 0, false, 0, null)] // SDE full week on approved leave = Compliant!
    [InlineData(4, 5, 0, 0, 1, 0, false, 0, null)] // SDE 4 P + 1 Exception + 0 A = Compliant!
    public void EvaluateWeeklyCompliance_CalculatesShortfallAndSeverityCorrectly(
        int actualPresent,
        int requiredDays,
        int approvedLeaveDays,
        int approvedWfhDays,
        int exceptionDays,
        int absentDays,
        bool expectedViolator,
        int expectedShortfall,
        ViolationSeverity? expectedSeverity)
    {
        var (isViolator, shortfall, severity) = _sut.EvaluateWeeklyCompliance(
            actualPresent, requiredDays, approvedLeaveDays, approvedWfhDays, exceptionDays, absentDays);

        Assert.Equal(expectedViolator, isViolator);
        Assert.Equal(expectedShortfall, shortfall);
        Assert.Equal(expectedSeverity, severity);
    }
}
