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
    /// Gets weekly WFO violators based on dynamic policy rules (SDE/Bench=5 days, A1+=3 days).
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
}
