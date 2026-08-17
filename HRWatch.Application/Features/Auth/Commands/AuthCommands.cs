using HRWatch.Application.Common;
using HRWatch.Application.Common.Auth;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Domain.Entities;
using HRWatch.Domain.Enums;
using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using BCryptNet = BCrypt.Net.BCrypt;

namespace HRWatch.Application.Features.Auth.Commands;

public record RegisterCommand(
    string Username,
    string Email,
    string Password,
    UserRole Role = UserRole.HR
) : ICommand<Result<AuthResponseDto>>;

public record LoginCommand(
    string UsernameOrEmail,
    string Password
) : ICommand<Result<AuthResponseDto>>;

public record AuthResponseDto(
    Guid UserId,
    string Username,
    string Email,
    string Role,
    string Token);

public class RegisterCommandHandler : ICommandHandler<RegisterCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtService;

    public RegisterCommandHandler(IApplicationDbContext dbContext, IJwtTokenService jwtService)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResponseDto>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            return Result<AuthResponseDto>.Failure("Username, Email, and Password are required.", "VALIDATION_ERROR");
        }

        var usernameExists = await _dbContext.Users.AnyAsync(u => u.Username.ToLower() == command.Username.ToLower(), cancellationToken);
        if (usernameExists)
        {
            return Result<AuthResponseDto>.Failure("Username is already taken.", "DUPLICATE_USERNAME");
        }

        var emailExists = await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == command.Email.ToLower(), cancellationToken);
        if (emailExists)
        {
            return Result<AuthResponseDto>.Failure("Email is already registered.", "DUPLICATE_EMAIL");
        }

        var user = new User
        {
            Username = command.Username.Trim(),
            Email = command.Email.Trim(),
            PasswordHash = BCryptNet.HashPassword(command.Password),
            Role = command.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var token = _jwtService.GenerateToken(user);
        return Result<AuthResponseDto>.Success(new AuthResponseDto(user.Id, user.Username, user.Email, user.Role.ToString(), token));
    }
}

public class LoginCommandHandler : ICommandHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtService;

    public LoginCommandHandler(IApplicationDbContext dbContext, IJwtTokenService jwtService)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResponseDto>> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var identifier = command.UsernameOrEmail?.Trim().ToLower() ?? string.Empty;
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => (u.Username.ToLower() == identifier || u.Email.ToLower() == identifier) && u.IsActive, cancellationToken);

        if (user == null || !BCryptNet.Verify(command.Password, user.PasswordHash))
        {
            return Result<AuthResponseDto>.Failure("Invalid username/email or password.", "INVALID_CREDENTIALS");
        }

        var token = _jwtService.GenerateToken(user);
        return Result<AuthResponseDto>.Success(new AuthResponseDto(user.Id, user.Username, user.Email, user.Role.ToString(), token));
    }
}
