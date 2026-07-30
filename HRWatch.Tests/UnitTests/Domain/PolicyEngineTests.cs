using HRWatch.Domain.Entities;
using HRWatch.Domain.Enums;
using HRWatch.Domain.Services;
using FluentAssertions;
using Xunit;

namespace HRWatch.Tests.UnitTests.Domain;

/// <summary>
/// UNIT TESTS for the PolicyEngine domain service.
/// 
/// Unit tests test ONE thing in isolation.
/// No database, no HTTP, no DI container — pure C# objects.
/// This is why domain services are pure functions: easy to test.
/// </summary>
public class PolicyEngineTests
{
    private readonly PolicyEngine _policyEngine = new();

    [Fact]
    public void EvaluateAttendance_WhenEmployeeExceedsLateArrivals_ShouldCreateViolation()
    {
        // ARRANGE — set up the scenario
        var employee = Employee.Create(
            "EXT001", "John", "Doe", "john@company.com",
            "Engineering", Role.Employee, DateTime.Today.AddYears(-1));

        var policy = Policy.Create(
            "Standard Policy",
            "Company standard attendance policy",
            """{ "maxLateArrivalsPerMonth": 3 }""",
            DateTime.Today.AddMonths(-6));

        var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var endDate   = startDate.AddMonths(1).AddDays(-1);

        // Create 4 late attendance records (exceeds limit of 3)
        var attendanceRecords = Enumerable.Range(1, 4)
            .Select(d => Attendance.Create(
                employee.Id,
                startDate.AddDays(d - 1),
                new TimeSpan(9, 20, 0), // 9:20 AM — late
                new TimeSpan(18, 0, 0),
                AttendanceStatus.Late))
            .ToList();

        // ACT — run the business logic
        var violations = _policyEngine.EvaluateAttendance(
            employee, attendanceRecords, [policy], startDate, endDate);

        // ASSERT — verify the expected outcome
        violations.Should().HaveCount(1);
        violations[0].Type.Should().Be(ViolationType.ExcessiveLateArrivals);
        violations[0].EmployeeId.Should().Be(employee.Id);
    }

    [Fact]
    public void EvaluateAttendance_WhenEmployeeIsWithinLimits_ShouldReturnNoViolations()
    {
        var employee = Employee.Create(
            "EXT002", "Jane", "Smith", "jane@company.com",
            "HR", Role.HRManager, DateTime.Today.AddYears(-2));

        var policy = Policy.Create(
            "Standard", "Policy", """{ "maxLateArrivalsPerMonth": 3 }""", DateTime.Today.AddMonths(-1));

        var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var endDate   = startDate.AddMonths(1).AddDays(-1);

        // Only 2 late arrivals (within limit)
        var records = Enumerable.Range(1, 2)
            .Select(d => Attendance.Create(
                employee.Id, startDate.AddDays(d - 1),
                new TimeSpan(9, 10, 0), new TimeSpan(18, 0, 0), AttendanceStatus.Late))
            .ToList();

        var violations = _policyEngine.EvaluateAttendance(
            employee, records, [policy], startDate, endDate);

        violations.Should().BeEmpty();
    }
}
