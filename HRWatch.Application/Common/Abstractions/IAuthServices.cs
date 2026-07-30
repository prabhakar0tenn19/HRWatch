using HRWatch.Domain.Entities;

namespace HRWatch.Application.Common.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
