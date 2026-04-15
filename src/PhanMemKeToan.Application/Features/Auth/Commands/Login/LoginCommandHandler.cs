using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Common.Exceptions;
using PhanMemKeToan.Domain.Entities;
using RefreshTokenEntity = PhanMemKeToan.Domain.Entities.RefreshToken;

namespace PhanMemKeToan.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<LoginCommandHandler> logger
) : IRequestHandler<LoginCommand, LoginResult>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var ipKey = $"login:failed:{request.IpAddress}";

        // 1. Check IP-level rate limit (failed attempts counter)
        var counterStr = await cache.GetStringAsync(ipKey, cancellationToken);
        if (int.TryParse(counterStr, out var ipFailCount) && ipFailCount >= MaxFailedAttempts)
        {
            logger.LogWarning("Login rate limited for IP {IpAddress}", request.IpAddress);
            throw new AccountLockedException(DateTimeOffset.UtcNow.Add(RateLimitWindow));
        }

        // 2. Load user (ignore tenant filter for login — tenant resolved after auth)
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        // 3. Check account deactivation BEFORE password verification (intentional UX - FR-009/FR-010)
        if (user is not null && !user.IsActive)
        {
            logger.LogInformation("Login attempt for deactivated account {Email}", request.Email);
            throw new AccountDeactivatedException();
        }

        // 4. Check per-user lockout
        if (user is not null && user.LockedUntil.HasValue && user.LockedUntil > DateTimeOffset.UtcNow)
        {
            throw new AccountLockedException(user.LockedUntil.Value);
        }

        // 5. Verify password (use constant-time comparison path even when user not found)
        var passwordValid = user is not null && passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!passwordValid)
        {
            // Increment IP counter
            var newCount = (ipFailCount + 1).ToString();
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = RateLimitWindow };
            await cache.SetStringAsync(ipKey, newCount, options, cancellationToken);

            // Increment per-user lockout counter
            if (user is not null)
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= MaxFailedAttempts)
                    user.LockedUntil = DateTimeOffset.UtcNow.Add(LockDuration);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogWarning("Failed login for {Email} from {IpAddress}", request.Email, request.IpAddress);
            throw new InvalidCredentialsException();
        }

        // 6. Reset counters on success
        user!.LastLoginAt = DateTimeOffset.UtcNow;
        user.FailedLoginCount = 0;
        user.LockedUntil = null;

        // 7. Build claims
        var roles = user.UserRoles
            .Where(ur => !ur.Role.IsDeleted)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();
        var permissions = user.UserRoles
            .Where(ur => !ur.Role.IsDeleted)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var jti = Guid.NewGuid().ToString();
        var claimsDto = new UserClaimsDto(user.Id, user.TenantId, user.Email, roles, permissions, jti);

        // 8. Generate tokens
        var accessToken = jwtService.GenerateAccessToken(claimsDto);
        var (refreshPlaintext, refreshHash) = jwtService.GenerateRefreshToken();

        var refreshTtlDays = int.TryParse(configuration["JwtSettings:RefreshTokenTtlDays"], out var d) ? d : 7;
        var refreshExpiry = DateTimeOffset.UtcNow.AddDays(refreshTtlDays);

        // 9. Persist refresh token
        dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshHash,
            TokenFamily = Guid.NewGuid(),
            ExpiresAt = refreshExpiry,
            IssuedAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        // 10. Structured auth event log (FR-033)
        logger.LogInformation(
            "AUTH_EVENT {EventType} userId={UserId} tenantId={TenantId} ip={IpAddress} success=true",
            "LOGIN", user.Id, user.TenantId, request.IpAddress);

        var accessTtlMinutes = int.TryParse(configuration["JwtSettings:AccessTokenTtlMinutes"], out var m) ? m : 15;
        return new LoginResult(accessToken, refreshPlaintext, request.RememberMe, DateTimeOffset.UtcNow.AddMinutes(accessTtlMinutes));
    }
}
