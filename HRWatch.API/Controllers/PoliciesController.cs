using HRWatch.Application.Features.Policies.Commands.CreateNewPolicyVersion;
using HRWatch.Application.Features.Policies.Queries.GetActivePolicy;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HRWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoliciesController : ControllerBase
{
    private readonly ICommandMediator _commandMediator;
    private readonly IQueryMediator _queryMediator;

    public PoliciesController(ICommandMediator commandMediator, IQueryMediator queryMediator)
    {
        _commandMediator = commandMediator;
        _queryMediator = queryMediator;
    }

    /// <summary>
    /// Gets the current active WFO compliance policy rules.
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActivePolicy(CancellationToken cancellationToken)
    {
        var result = await _queryMediator.QueryAsync(new GetActivePolicyQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets the full historical audit trail of all policy versions (active and archived).
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetPolicyHistory(CancellationToken cancellationToken)
    {
        var result = await _queryMediator.QueryAsync(new HRWatch.Application.Features.Policies.Queries.GetPolicyHistory.GetPolicyHistoryQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(result.Value);
    }

    
    /// Activates a new version of WFO policy and archives the previous version.
   
    [HttpPost("new-version")]
    public async Task<IActionResult> CreateNewVersion([FromBody] CreateNewPolicyVersionCommand command, CancellationToken cancellationToken)
    {
        var result = await _commandMediator.SendAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(new { PolicyId = result.Value, Message = "New policy version activated successfully." });
    }
}
