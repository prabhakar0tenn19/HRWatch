using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Attendance.Commands.SyncAttendance;
using HRWatch.Application.Features.Attendance.Queries.GetAttendance;
using Microsoft.AspNetCore.Mvc;

namespace HRWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AttendanceController : ControllerBase
{
    private readonly ICommandMediator _commandMediator;
    private readonly IQueryMediator _queryMediator;

    public AttendanceController(ICommandMediator commandMediator, IQueryMediator queryMediator)
    {
        _commandMediator = commandMediator;
        _queryMediator = queryMediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAttendanceQuery
        {
            EmployeeId = employeeId,
            FromDate = fromDate ?? DateTime.UtcNow.AddDays(-7),
            ToDate = toDate ?? DateTime.UtcNow.Date
        };

        var result = await _queryMediator.SendAsync(query, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error.Message, code = result.Error.Code });
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncAttendance(
        [FromBody] SyncAttendanceCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _commandMediator.SendAsync(command, cancellationToken);
        return result.IsSuccess
            ? Ok(new { message = "Attendance sync completed.", data = result.Value })
            : BadRequest(new { error = result.Error.Message, code = result.Error.Code });
    }
}
