using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Auth.Commands.LoginUser;
using HRWatch.Domain.Enums;

namespace HRWatch.Application.Features.Auth.Commands.RegisterUser;

public record RegisterUserCommand : ICommand<AuthResultDto>
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public UserRole Role { get; init; } = UserRole.HR;
}
