using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Violations.Queries.GetWeeklyViolators;
using Microsoft.AspNetCore.Mvc;

namespace HRWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ViolationsController : ControllerBase
{
    private readonly IQueryMediator _queryMediator;

    public ViolationsController(IQueryMediator queryMediator)
    {
        _queryMediator = queryMediator;
    }

    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeeklyViolators(
        [FromQuery] DateTime? weekStartDate = null,
        [FromQuery] string? designation = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetWeeklyViolatorsQuery(weekStartDate, designation);
        var result = await _queryMediator.SendAsync(query, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error.Message, code = result.Error.Code });
    }
}
