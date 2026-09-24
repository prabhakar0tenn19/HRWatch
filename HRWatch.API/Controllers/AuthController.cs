using HRWatch.Application.Features.Auth.Commands;
using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HRWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ICommandMediator _commandMediator;

    public AuthController(ICommandMediator commandMediator)
    {
        _commandMediator = commandMediator;
    }

    
    /// Authenticates HR / Admin user and returns JWT token.
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _commandMediator.SendAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Unauthorized(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(result.Value);
    }

 
    /// Registers a new HR / Admin user (SuperAdmin only in production).
   
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _commandMediator.SendAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { result.ErrorMessage, result.ErrorCode });
        }

        return Ok(result.Value);
    }
}
