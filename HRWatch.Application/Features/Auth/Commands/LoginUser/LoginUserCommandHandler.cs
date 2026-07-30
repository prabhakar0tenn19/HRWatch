using HRWatch.Application.Common;
using HRWatch.Application.Common.Abstractions;

namespace HRWatch.Application.Features.Auth.Commands.LoginUser;

public class LoginUserCommandHandler : ICommandHandler<LoginUserCommand, AuthResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResultDto>> HandleAsync(LoginUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(command.UsernameOrEmail, cancellationToken);
        if (user is null)
        {
            user = await _userRepository.GetByEmailAsync(command.UsernameOrEmail, cancellationToken);
        }

        if (user is null)
        {
            return Result<AuthResultDto>.Failure("INVALID_CREDENTIALS", "Invalid username or password.");
        }

        if (!user.IsActive)
        {
            return Result<AuthResultDto>.Failure("ACCOUNT_DISABLED", "User account is disabled.");
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(command.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return Result<AuthResultDto>.Failure("INVALID_CREDENTIALS", "Invalid username or password.");
        }

        string token = _jwtTokenGenerator.GenerateToken(user);

        var result = new AuthResultDto(
            user.Id,
            user.Username,
            user.Email,
            user.Role.ToString(),
            token);

        return Result<AuthResultDto>.Success(result);
    }
}
