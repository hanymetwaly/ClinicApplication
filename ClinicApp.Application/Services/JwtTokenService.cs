using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClinicApp.Application.Interfaces;
using ClinicApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClinicApp.Application.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IClinicDbContext _context;
    private readonly IConfiguration _configuration;

    public JwtTokenService(IClinicDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "ClinicApp";
        var audience = _configuration["Jwt:Audience"] ?? "ClinicAppUsers";
        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes", 60);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role?.Name ?? throw new InvalidOperationException("User role is not loaded"))
        };

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId)
    {
        var token = GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _context.AddAsync(refreshToken);
        await _context.SaveChangesAsync();
        return refreshToken;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt =>
                rt.Token == token &&
                !rt.IsRevoked &&
                rt.ExpiresAt > DateTime.UtcNow);
        return refreshToken != null;
    }

    public async Task<(User User, RefreshToken RefreshToken)?> RotateRefreshTokenAsync(string token)
    {
        User? user = null;
        RefreshToken? replacement = null;

        await _context.ExecuteInTransactionAsync(async cancellationToken =>
        {
            var current = await _context.RefreshTokens
                .Include(refreshToken => refreshToken.User)
                .ThenInclude(tokenUser => tokenUser!.Role)
                .FirstOrDefaultAsync(refreshToken =>
                    refreshToken.Token == token &&
                    !refreshToken.IsRevoked &&
                    refreshToken.ExpiresAt > DateTime.UtcNow,
                    cancellationToken);

            if (current?.User is null || !current.User.IsActive)
            {
                return;
            }

            current.IsRevoked = true;
            current.RevokedAt = DateTime.UtcNow;
            user = current.User;
            replacement = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = current.UserId,
                Token = GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            await _context.AddAsync(replacement);
            await _context.SaveChangesAsync(cancellationToken);
        });

        return user is null || replacement is null ? null : (user, replacement);
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
        if (refreshToken != null)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
