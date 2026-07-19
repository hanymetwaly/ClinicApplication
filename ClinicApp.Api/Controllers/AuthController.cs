using ClinicApp.Application.DTOs;
using ClinicApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicApp.Api.Controllers;

/// <summary>
/// Provides authentication endpoints for login, token refresh, and logout.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class AuthController(
    IClinicService clinicService,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    /// <summary>
    /// Authenticates a user and returns JWT access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await clinicService.AuthenticateAsync(request.Username, request.Password);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid username or password."
            });
        }

        var refreshToken = await jwtTokenService.CreateRefreshTokenAsync(user.Id);
        return Ok(CreateResponse(user.Username, user.Role!.Name, jwtTokenService.GenerateAccessToken(user), refreshToken.Token));
    }

    /// <summary>
    /// Rotates an existing refresh token and issues a new access token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(RefreshTokenRequest request)
    {
        var result = await jwtTokenService.RotateRefreshTokenAsync(request.RefreshToken);
        if (result is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid or expired refresh token."
            });
        }

        var (user, refreshToken) = result.Value;
        return Ok(CreateResponse(user.Username, user.Role!.Name, jwtTokenService.GenerateAccessToken(user), refreshToken.Token));
    }

    /// <summary>
    /// Revokes an existing refresh token so it cannot be used again.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request)
    {
        await jwtTokenService.RevokeRefreshTokenAsync(request.RefreshToken);
        return NoContent();
    }

    private static LoginResponse CreateResponse(
        string username,
        string role,
        string accessToken,
        string refreshToken) => new()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Username = username,
            Role = role
        };
}
