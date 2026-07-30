using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Auth.Commands.LoginUser;

public record LoginUserCommand : ICommand<AuthResultDto>
{
    public string UsernameOrEmail { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record AuthResultDto(
    Guid UserId,
    string Username,
    string Email,
    string Role,
    string Token);
