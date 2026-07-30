using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Auth.Commands.LoginUser;
using HRWatch.Application.Features.Auth.Commands.RegisterUser;
using Microsoft.AspNetCore.Mvc;

namespace HRWatch.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ICommandMediator _commandMediator;

    public AuthController(ICommandMediator commandMediator)
    {
        _commandMediator = commandMediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _commandMediator.SendAsync(command, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error.Message, code = result.Error.Code });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _commandMediator.SendAsync(command, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error.Message, code = result.Error.Code });
    }
}
