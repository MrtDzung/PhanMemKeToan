using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhanMemKeToan.Application.Features.Auth.Commands.Login;
using PhanMemKeToan.Application.Features.Auth.Commands.Logout;
using PhanMemKeToan.Application.Features.Auth.Commands.RefreshToken;
using PhanMemKeToan.Application.Features.Auth.Commands.SelectCompany;
using PhanMemKeToan.Application.Features.Auth.Commands.SwitchCompany;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PhanMemKeToan.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";

    /// <summary>Step 1: Validate credentials, return tempToken + companies list.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await sender.Send(new LoginCommand(request.Email, request.Password, request.RememberMe, ip), cancellationToken);

        return Ok(new
        {
            tempToken = result.TempToken,
            companies = result.Companies,
            rememberMe = result.RememberMe
        });
    }

    /// <summary>Step 2: Select company, issue JWT + refresh cookie.</summary>
    [HttpPost("select-company")]
    [AllowAnonymous]
    public async Task<IActionResult> SelectCompany([FromBody] SelectCompanyRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await sender.Send(
            new SelectCompanyCommand(request.TempToken, request.TenantId, request.RememberMe, ip), cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken, request.RememberMe);
        return Ok(new { accessToken = result.AccessToken, expiresAt = result.ExpiresAt });
    }

    /// <summary>Switch to another company without re-login.</summary>
    [HttpPost("switch-company")]
    [Authorize]
    public async Task<IActionResult> SwitchCompany([FromBody] SwitchCompanyRequest request, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty;
        var currentTenantStr = User.FindFirst("tid")?.Value ?? string.Empty;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        Guid.TryParse(userIdStr, out var userId);
        Guid.TryParse(currentTenantStr, out var currentTenantId);

        var result = await sender.Send(
            new SwitchCompanyCommand(userId, currentTenantId, request.TargetTenantId, ip), cancellationToken);

        SetRefreshTokenCookie(result.RefreshToken, false);
        return Ok(new { accessToken = result.AccessToken, expiresAt = result.ExpiresAt });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { errorCode = "TOKEN_MISSING" });

        var result = await sender.Send(new RefreshTokenCommand(refreshToken), cancellationToken);
        SetRefreshTokenCookie(result.RefreshToken, false);
        return Ok(new { accessToken = result.AccessToken, expiresAt = result.ExpiresAt });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty;
        var tenantIdStr = User.FindFirst("tid")?.Value ?? string.Empty;

        Guid.TryParse(userIdStr, out var userId);
        Guid.TryParse(tenantIdStr, out var tenantId);

        // Compute refresh token hash
        var refreshToken = Request.Cookies[RefreshTokenCookieName] ?? string.Empty;
        var tokenHash = string.Empty;
        if (!string.IsNullOrEmpty(refreshToken))
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(refreshToken));
            tokenHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        // Calculate remaining TTL from token expiry
        var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        var remainingTtl = TimeSpan.Zero;
        if (long.TryParse(expClaim, out var expUnix))
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(expUnix);
            remainingTtl = expiry - DateTimeOffset.UtcNow;
        }

        await sender.Send(new LogoutCommand(jti, remainingTtl, tokenHash, userId, tenantId), cancellationToken);

        Response.Cookies.Delete(RefreshTokenCookieName);
        return NoContent();
    }

    private void SetRefreshTokenCookie(string refreshToken, bool persistent)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth"
        };
        if (persistent)
            options.MaxAge = TimeSpan.FromDays(7);
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, options);
    }
}

public record LoginRequest(string Email, string Password, bool RememberMe);
public record SelectCompanyRequest(string TempToken, Guid TenantId, bool RememberMe);
public record SwitchCompanyRequest(Guid TargetTenantId);
