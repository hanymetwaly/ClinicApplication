using ClinicApp.Application.Interfaces;
using ClinicApp.Application.Services;
using ClinicApp.Domain.Entities;
using ClinicApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClinicApp.Api.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateAccessToken_ReturnsValidToken()
    {
        var (context, configuration) = CreateTestContext();
        var service = new JwtTokenService(context, configuration);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            RoleId = 1,
            Role = new Role { Name = RoleNames.Admin },
            IsActive = true
        };

        var token = service.GenerateAccessToken(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        var (context, configuration) = CreateTestContext();
        var service = new JwtTokenService(context, configuration);

        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_SavesToDatabase()
    {
        var (context, configuration) = CreateTestContext();
        await using (context)
        {
            var service = new JwtTokenService(context, configuration);
            var userId = Guid.NewGuid();

            var refreshToken = await service.CreateRefreshTokenAsync(userId);

            Assert.NotNull(refreshToken);
            Assert.Equal(userId, refreshToken.UserId);
            Assert.False(refreshToken.IsRevoked);

            var saved = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken.Token);
            Assert.NotNull(saved);
        }
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ValidToken_ReturnsTrue()
    {
        var (context, configuration) = CreateTestContext();
        await using (context)
        {
            var service = new JwtTokenService(context, configuration);
            var userId = Guid.NewGuid();

            var refreshToken = await service.CreateRefreshTokenAsync(userId);
            var isValid = await service.ValidateRefreshTokenAsync(refreshToken.Token);

            Assert.True(isValid);
        }
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_RevokedToken_ReturnsFalse()
    {
        var (context, configuration) = CreateTestContext();
        await using (context)
        {
            var service = new JwtTokenService(context, configuration);
            var userId = Guid.NewGuid();

            var refreshToken = await service.CreateRefreshTokenAsync(userId);
            await service.RevokeRefreshTokenAsync(refreshToken.Token);

            var isValid = await service.ValidateRefreshTokenAsync(refreshToken.Token);

            Assert.False(isValid);
        }
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ExpiredToken_ReturnsFalse()
    {
        var (context, configuration) = CreateTestContext();
        await using (context)
        {
            var service = new JwtTokenService(context, configuration);
            var userId = Guid.NewGuid();

            var refreshToken = await service.CreateRefreshTokenAsync(userId);
            refreshToken.ExpiresAt = DateTime.UtcNow.AddDays(-1);
            await context.SaveChangesAsync();

            var isValid = await service.ValidateRefreshTokenAsync(refreshToken.Token);

            Assert.False(isValid);
        }
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_MarksAsRevoked()
    {
        var (context, configuration) = CreateTestContext();
        await using (context)
        {
            var service = new JwtTokenService(context, configuration);
            var userId = Guid.NewGuid();

            var refreshToken = await service.CreateRefreshTokenAsync(userId);
            await service.RevokeRefreshTokenAsync(refreshToken.Token);

            var revoked = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken.Token);
            Assert.NotNull(revoked);
            Assert.True(revoked.IsRevoked);
            Assert.NotNull(revoked.RevokedAt);
        }
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_NonExistentToken_DoesNothing()
    {
        var (context, configuration) = CreateTestContext();
        await using (context)
        {
            var service = new JwtTokenService(context, configuration);

            await service.RevokeRefreshTokenAsync("nonexistent-token");

            // Should not throw exception
        }
    }

    private static (IClinicDbContext Context, IConfiguration Configuration) CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ClinicDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-with-at-least-thirty-two-bytes",
                ["Jwt:Issuer"] = "ClinicApp",
                ["Jwt:Audience"] = "ClinicAppUsers",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();

        return (context, configuration);
    }
}
