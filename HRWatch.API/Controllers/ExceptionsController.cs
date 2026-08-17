using HRWatch.Application.Features.Exceptions.Commands.CreateException;
using HRWatch.Application.Features.Exceptions.Commands.RevokeException;
using HRWatch.Application.Features.Exceptions.Queries.GetActiveExceptions;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HRWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExceptionsController : ControllerBase
{
    private readonly ICommandMediator _commandMediator;
    private readonly IQueryMediator _queryMediator;

    public ExceptionsController(ICommandMediator commandMediator, IQueryMediator queryMediator)
    {
        _commandMediator = commandMediator;
        _queryMediator = queryMediator;
    }

    /// <summary>
    /// Creates a new WFH / on-site exception with overlapping date range validation.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateException([FromBody] CreateExceptionCommand command, CancellationToken cancellationToken)
    {
        var result = await _commandMediator.SendAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(new { ExceptionId = result.Value, Message = "Exception created successfully." });
    }

    /// <summary>
    /// Soft-revokes an active exception (IsActive = false).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeException(Guid id, CancellationToken cancellationToken)
    {
        var result = await _commandMediator.SendAsync(new RevokeExceptionCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(new { Message = "Exception revoked successfully." });
    }

    /// <summary>
    /// Lists active exceptions (or all history).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetExceptions([FromQuery] Guid? employeeId, [FromQuery] bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var result = await _queryMediator.QueryAsync(new GetActiveExceptionsQuery(employeeId, activeOnly), cancellationToken);
        return Ok(result.Value);
    }
}
