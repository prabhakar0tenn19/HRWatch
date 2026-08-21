using HRWatch.Application.Features.Violations.Queries.GetPastWeeksSummary;
using HRWatch.Application.Features.Violations.Queries.GetWeeklyViolators;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HRWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ViolationsController : ControllerBase
{
    private readonly IQueryMediator _queryMediator;

    public ViolationsController(IQueryMediator queryMediator)
    {
        _queryMediator = queryMediator;
    }

    /// <summary>
    /// Gets weekly WFO violators for a single week based on dynamic policy rules.
    /// Automatically normalizes any provided date to its week's Monday.
    /// </summary>
    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeeklyViolators(
        [FromQuery] DateOnly? weekStartDate,
        [FromQuery] string? designation,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        var result = await _queryMediator.QueryAsync(
            new GetWeeklyViolatorsQuery(weekStartDate, designation, searchTerm),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets aggregated summary of past N weeks (default 4 weeks) for the Dashboard,
    /// including week cards (with total & critical violators) and the Top 5 Shortfall widget.
    /// </summary>
    [HttpGet("summary-past-weeks")]
    public async Task<IActionResult> GetPastWeeksSummary(
        [FromQuery] int weeksCount = 4,
        [FromQuery] string? designation = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _queryMediator.QueryAsync(
            new GetPastWeeksSummaryQuery(weeksCount, designation, searchTerm),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(result.Value);
    }
}
