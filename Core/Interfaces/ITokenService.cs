using Core.Entities;

namespace Core.Interfaces;

public interface ITokenService
{
    string CreateToken(int userId, string email, string displayName, IList<string> roles);
    string GenerateRefreshToken();
    Task<RefreshToken?> ValidateRefreshTokenAsync(string token);
}
