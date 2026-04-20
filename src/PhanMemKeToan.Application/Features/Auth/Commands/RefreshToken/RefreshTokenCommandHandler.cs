using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Common.Exceptions;
using RefreshTokenEntity = PhanMemKeToan.Domain.Entities.RefreshToken;

namespace PhanMemKeToan.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IMasterDbContext masterDbContext,
    IApplicationDbContext tenantDbContext,
    IJwtService jwtService,
    IConfiguration configuration,
    ILogger<RefreshTokenCommandHandler> logger
) : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshTokenPlaintext))).ToLowerInvariant();

        // 1. Find token in Master DB
        var existingToken = await masterDbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null)
            throw new TokenExpiredException();

        // 2. Replay detection
        if (existingToken.IsRevoked)
        {
            logger.LogWarning(
                "AUTH_EVENT {EventType} userId={UserId} tenantId={TenantId} tokenFamily={Family} isReplay=true",
                "REFRESH", existingToken.UserId, existingToken.TenantId, existingToken.TokenFamily);

            var familyTokens = await masterDbContext.RefreshTokens
                .Where(rt => rt.TokenFamily == existingToken.TokenFamily && !rt.IsRevoked)
                .ToListAsync(cancellationToken);
            foreach (var t in familyTokens)
                t.IsRevoked = true;
            await masterDbContext.SaveChangesAsync(cancellationToken);
            throw new TokenRevokedException();
        }

        // 3. Check expiry
        if (existingToken.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new TokenExpiredException();

        // 4. Revoke old token
        existingToken.IsRevoked = true;

        // 5. Load user from Tenant DB for roles/permissions
        var user = await tenantDbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == existingToken.UserId && u.TenantId == existingToken.TenantId, cancellationToken)
            ?? throw new TokenExpiredException();

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
        var claimsDto = new UserClaimsDto(user.Id, existingToken.TenantId, user.Email, roles, permissions, jti);

        // 6. Generate new pair
        var accessToken = jwtService.GenerateAccessToken(claimsDto);
        var (refreshPlaintext, refreshHash) = jwtService.GenerateRefreshToken();

        var refreshTtlDays = int.TryParse(configuration["JwtSettings:RefreshTokenTtlDays"], out var d) ? d : 7;
        var refreshExpiry = DateTimeOffset.UtcNow.AddDays(refreshTtlDays);

        masterDbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = existingToken.TenantId,
            TokenHash = refreshHash,
            TokenFamily = existingToken.TokenFamily,
            ExpiresAt = refreshExpiry,
            IssuedAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        });

        await masterDbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "AUTH_EVENT {EventType} userId={UserId} tenantId={TenantId} isReplay=false success=true",
            "REFRESH", user.Id, existingToken.TenantId);

        var accessTtlMinutes = int.TryParse(configuration["JwtSettings:AccessTokenTtlMinutes"], out var m) ? m : 15;
        return new RefreshTokenResult(accessToken, refreshPlaintext, DateTimeOffset.UtcNow.AddMinutes(accessTtlMinutes));
    }
}
