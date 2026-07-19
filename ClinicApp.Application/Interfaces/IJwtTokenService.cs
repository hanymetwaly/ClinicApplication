using ClinicApp.Domain.Entities;

namespace ClinicApp.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId);
    Task<bool> ValidateRefreshTokenAsync(string token);
    Task<(User User, RefreshToken RefreshToken)?> RotateRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
}
