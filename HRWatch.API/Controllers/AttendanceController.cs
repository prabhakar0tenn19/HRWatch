using HRWatch.Application.Features.Attendance.Commands.EvaluateDailyAttendance;
using HRWatch.Application.Features.Attendance.Commands.SyncEmployees;
using HRWatch.Application.Features.Attendance.Queries.GetDailyAttendanceCalendar;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HRWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly ICommandMediator _commandMediator;
    private readonly IQueryMediator _queryMediator;

    public AttendanceController(ICommandMediator commandMediator, IQueryMediator queryMediator)
    {
        _commandMediator = commandMediator;
        _queryMediator = queryMediator;
    }

    /// <summary>
    /// Synchronizes active India employees from CG1 Master API into the local Employees table.
    /// </summary>
    [HttpPost("sync-employees")]
    public async Task<IActionResult> SyncEmployees(CancellationToken cancellationToken)
    {
        var result = await _commandMediator.SendAsync(new SyncEmployeesCommand("ManualApiTrigger"), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Evaluates daily attendance for a specific date (or today).
    /// Extracts COSEC biometric punches -> queries CG1 Leave API for non-punched employees -> verifies Exceptions -> records DailyAttendance.
    /// </summary>
    [HttpPost("evaluate-daily")]
    public async Task<IActionResult> EvaluateDaily([FromQuery] DateOnly? targetDate, CancellationToken cancellationToken)
    {
        var result = await _commandMediator.SendAsync(new EvaluateDailyAttendanceCommand(targetDate, "ManualApiTrigger"), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Evaluates attendance for a whole date range (e.g. from Monday to Friday), fetches biometric logs & leaves for each day, and stores all records in DB.
    /// </summary>
    [HttpPost("evaluate-range")]
    public async Task<IActionResult> EvaluateRange(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        if (startDate > endDate)
        {
            return BadRequest(new { ErrorMessage = "startDate cannot be after endDate" });
        }

        var result = await _commandMediator.SendAsync(
            new HRWatch.Application.Features.Attendance.Commands.EvaluateDateRange.EvaluateDateRangeCommand(startDate, endDate, "ManualApiTrigger"),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns the calendar view (P, L, E, A, WO, H) for employees within a date range.
    /// </summary>
    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] string? designation,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        if (startDate > endDate)
        {
            return BadRequest(new { ErrorMessage = "startDate cannot be after endDate" });
        }

        var result = await _queryMediator.QueryAsync(
            new GetDailyAttendanceCalendarQuery(startDate, endDate, designation, searchTerm),
            cancellationToken);

        return Ok(result.Value);
    }
}
