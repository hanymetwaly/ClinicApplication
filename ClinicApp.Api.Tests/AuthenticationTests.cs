using ClinicApp.Application.Interfaces;
using ClinicApp.Application.Services;
using ClinicApp.Domain.Entities;
using ClinicApp.Infrastructure.Data;
using ClinicApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClinicApp.Api.Tests;

public class AuthenticationTests
{
    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsUser()
    {
        var (context, configuration) = CreateTestContext();
        await using (context)
        {
            var passwordHasher = new PasswordHasher();
            var hashedPassword = passwordHasher.HashPassword("testpassword");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = hashedPassword,
                RoleId = 1,
                Role = new Role { Name = RoleNames.Admin },
                IsActive = true
            };
            await context.AddAsync(user);
            await context.SaveChangesAsync();

            var service = new ClinicService(
                context,
                NullLogger<ClinicService>.Instance,
                new PatientRepository(context),
                new AppointmentRepository(context),
                new InvoiceRepository(context),
                passwordHasher,
                new TestFileStorageService());

            var result = await service.AuthenticateAsync("testuser", "testpassword");

            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
            Assert.Equal(RoleNames.Admin, result.Role?.Name);
        }
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidPassword_ReturnsNull()
    {
        var (context, configuration) = CreateTestContext();
        await using (context)
        {
            var passwordHasher = new PasswordHasher();
            var hashedPassword = passwordHasher.HashPassword("testpassword");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = hashedPassword,
                RoleId = 1,
                Role = new Role { Name = RoleNames.Admin },
                IsActive = true
            };
            await context.AddAsync(user);
            await context.SaveChangesAsync();

            var service = new ClinicService(
                context,
                NullLogger<ClinicService>.Instance,
                new PatientRepository(context),
                new AppointmentRepository(context),
                new InvoiceRepository(context),
                passwordHasher,
                new TestFileStorageService());

            var result = await service.AuthenticateAsync("testuser", "wrongpassword");

            Assert.Null(result);
        }
    }

    [Fact]
    public async Task AuthenticateAsync_NonExistentUser_ReturnsNull()
    {
        var (context, configuration) = CreateTestContext();
        await using (context)
        {
            var passwordHasher = new PasswordHasher();
            var service = new ClinicService(
                context,
                NullLogger<ClinicService>.Instance,
                new PatientRepository(context),
                new AppointmentRepository(context),
                new InvoiceRepository(context),
                passwordHasher,
                new TestFileStorageService());

            var result = await service.AuthenticateAsync("nonexistent", "password");

            Assert.Null(result);
        }
    }

    [Fact]
    public async Task AuthenticateAsync_InactiveUser_ReturnsNull()
    {
        var (context, configuration) = CreateTestContext();
        await using (context)
        {
            var passwordHasher = new PasswordHasher();
            var hashedPassword = passwordHasher.HashPassword("testpassword");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = hashedPassword,
                RoleId = 1,
                Role = new Role { Name = RoleNames.Admin },
                IsActive = false
            };
            await context.AddAsync(user);
            await context.SaveChangesAsync();

            var service = new ClinicService(
                context,
                NullLogger<ClinicService>.Instance,
                new PatientRepository(context),
                new AppointmentRepository(context),
                new InvoiceRepository(context),
                passwordHasher,
                new TestFileStorageService());

            var result = await service.AuthenticateAsync("testuser", "testpassword");

            Assert.Null(result);
        }
    }

    [Fact]
    public void PasswordHasher_HashAndVerify_ReturnsTrue()
    {
        var hasher = new PasswordHasher();
        var password = "testpassword123";

        var hash = hasher.HashPassword(password);
        var result = hasher.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void PasswordHasher_VerifyWrongPassword_ReturnsFalse()
    {
        var hasher = new PasswordHasher();
        var password = "testpassword123";

        var hash = hasher.HashPassword(password);
        var result = hasher.VerifyPassword("wrongpassword", hash);

        Assert.False(result);
    }

    [Fact]
    public void PasswordHasher_HashSamePasswordTwice_DifferentHashes()
    {
        var hasher = new PasswordHasher();
        var password = "testpassword123";

        var hash1 = hasher.HashPassword(password);
        var hash2 = hasher.HashPassword(password);

        Assert.NotEqual(hash1, hash2);
    }

    private static (ClinicDbContext Context, IConfiguration Configuration) CreateTestContext()
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
