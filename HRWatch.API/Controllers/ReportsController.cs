using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Compliance.Commands.EvaluateCompliance;
using HRWatch.Application.Features.Reports.Commands.GenerateWeeklyReport;
using HRWatch.Application.Features.Reports.Queries.GetLatestWeeklyReport;
using Microsoft.AspNetCore.Mvc;

namespace HRWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly ICommandMediator _commandMediator;
    private readonly IQueryMediator _queryMediator;

    public ReportsController(ICommandMediator commandMediator, IQueryMediator queryMediator)
    {
        _commandMediator = commandMediator;
        _queryMediator = queryMediator;
    }

    [HttpGet("weekly/latest")]
    public async Task<IActionResult> GetLatestWeeklyReport(CancellationToken cancellationToken = default)
    {
        var result = await _queryMediator.SendAsync(new GetLatestWeeklyReportQuery(), cancellationToken);
        return result.IsSuccess
            ? (result.Value is null ? NotFound("No reports generated yet.") : Ok(result.Value))
            : BadRequest(new { error = result.Error.Message, code = result.Error.Code });
    }

    [HttpPost("weekly/generate")]
    public async Task<IActionResult> GenerateWeeklyReport(
        [FromBody] GenerateWeeklyReportCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _commandMediator.SendAsync(command, cancellationToken);
        return result.IsSuccess
            ? Ok(new { reportId = result.Value, message = "Report generated successfully." })
            : BadRequest(new { error = result.Error.Message, code = result.Error.Code });
    }

    [HttpPost("compliance/evaluate")]
    public async Task<IActionResult> EvaluateCompliance(
        [FromBody] EvaluateComplianceCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _commandMediator.SendAsync(command, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error.Message, code = result.Error.Code });
    }
}
