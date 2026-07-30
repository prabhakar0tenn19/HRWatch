using HRWatch.Application.Common;
using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Auth.Commands.LoginUser;
using HRWatch.Domain.Entities;

namespace HRWatch.Application.Features.Auth.Commands.RegisterUser;

public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, AuthResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResultDto>> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        bool exists = await _userRepository.ExistsByUsernameOrEmailAsync(command.Username, command.Email, cancellationToken);
        if (exists)
        {
            return Result<AuthResultDto>.Failure("USER_EXISTS", "Username or email is already registered.");
        }

        string passwordHash = _passwordHasher.HashPassword(command.Password);

        var user = User.Create(command.Username, command.Email, passwordHash, command.Role);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

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
