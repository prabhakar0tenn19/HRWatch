using HRWatch.Domain.Entities;

namespace HRWatch.Application.Common.Auth;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
