using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Auth.Commands.Login;
using PhanMemKeToan.Domain.Common.Exceptions;
using PhanMemKeToan.Domain.Entities;
using RefreshTokenEntity = PhanMemKeToan.Domain.Entities.RefreshToken;

namespace PhanMemKeToan.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IApplicationDbContext dbContext,
    IJwtService jwtService,
    IConfiguration configuration,
    ILogger<RefreshTokenCommandHandler> logger
) : IRequestHandler<RefreshTokenCommand, LoginResult>
{
    public async Task<LoginResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshTokenPlaintext))).ToLowerInvariant();

        // 1. Find token by hash (ignore tenant filter)
        var existingToken = await dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null)
            throw new TokenExpiredException();

        // 2. Replay detection: token already revoked = family attack
        if (existingToken.IsRevoked)
        {
            logger.LogWarning(
                "AUTH_EVENT {EventType} userId={UserId} tenantId={TenantId} tokenFamily={Family} isReplay=true success=false",
                "REFRESH", existingToken.UserId, existingToken.User.TenantId, existingToken.TokenFamily);

            // Revoke entire token family
            var familyTokens = await dbContext.RefreshTokens
                .IgnoreQueryFilters()
                .Where(rt => rt.TokenFamily == existingToken.TokenFamily && !rt.IsRevoked)
                .ToListAsync(cancellationToken);
            foreach (var t in familyTokens)
                t.IsRevoked = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new TokenRevokedException();
        }

        // 3. Check expiry
        if (existingToken.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new TokenExpiredException();

        // 4. Revoke old token
        existingToken.IsRevoked = true;

        var user = existingToken.User;
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

        // 5. Generate new pair
        var accessToken = jwtService.GenerateAccessToken(claimsDto);
        var (refreshPlaintext, refreshHash) = jwtService.GenerateRefreshToken();

        var refreshTtlDays = int.TryParse(configuration["JwtSettings:RefreshTokenTtlDays"], out var d) ? d : 7;
        var refreshExpiry = DateTimeOffset.UtcNow.AddDays(refreshTtlDays);

        dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshHash,
            TokenFamily = existingToken.TokenFamily, // Keep same family
            ExpiresAt = refreshExpiry,
            IssuedAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        // 6. FR-033 auth log
        logger.LogInformation(
            "AUTH_EVENT {EventType} userId={UserId} tenantId={TenantId} isReplay=false success=true",
            "REFRESH", user.Id, user.TenantId);

        var accessTtlMinutes = int.TryParse(configuration["JwtSettings:AccessTokenTtlMinutes"], out var m) ? m : 15;
        return new LoginResult(accessToken, refreshPlaintext, false, DateTimeOffset.UtcNow.AddMinutes(accessTtlMinutes));
    }
}
