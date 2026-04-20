using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Common.Exceptions;
using RefreshTokenEntity = PhanMemKeToan.Domain.Entities.RefreshToken;

namespace PhanMemKeToan.Application.Features.Auth.Commands.SelectCompany;

public class SelectCompanyCommandHandler(
    IMasterDbContext masterDbContext,
    IApplicationDbContext tenantDbContext,
    ITempTokenService tempTokenService,
    IJwtService jwtService,
    IConfiguration configuration,
    ILogger<SelectCompanyCommandHandler> logger
) : IRequestHandler<SelectCompanyCommand, SelectCompanyResult>
{
    public async Task<SelectCompanyResult> Handle(SelectCompanyCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate tempToken
        var claims = tempTokenService.Validate(request.TempToken)
            ?? throw new UnauthorizedException("INVALID_TEMP_TOKEN", "Temporary token is invalid or expired.");

        // 2. Verify user has access to the requested tenant
        var access = await masterDbContext.MasterUserTenants
            .AnyAsync(mut => mut.MasterUserId == claims.UserId && mut.TenantId == request.TenantId, cancellationToken);

        if (!access)
            throw new ForbiddenAccessException();

        // 3. Verify tenant is active and online
        var tenant = await masterDbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId && t.IsActive, cancellationToken)
            ?? throw new NotFoundException("TENANT_NOT_FOUND", $"Tenant {request.TenantId} not found or inactive.");

        // 4. Load user from Tenant DB to get roles/permissions
        var user = await tenantDbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == claims.UserId && u.TenantId == request.TenantId, cancellationToken)
            ?? throw new NotFoundException("USER_NOT_FOUND", "User not found in selected company.");

        // 5. Build claims from tenant-scoped roles
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
        var claimsDto = new UserClaimsDto(user.Id, request.TenantId, user.Email, roles, permissions, jti);

        // 6. Generate JWT + refresh token
        var accessToken = jwtService.GenerateAccessToken(claimsDto);
        var (refreshPlaintext, refreshHash) = jwtService.GenerateRefreshToken();

        var refreshTtlDays = int.TryParse(configuration["JwtSettings:RefreshTokenTtlDays"], out var d) ? d : 7;
        var refreshExpiry = DateTimeOffset.UtcNow.AddDays(refreshTtlDays);

        // 7. Persist refresh token in Master DB
        masterDbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = claims.UserId,
            TenantId = request.TenantId,
            TokenHash = refreshHash,
            TokenFamily = Guid.NewGuid(),
            ExpiresAt = refreshExpiry,
            IssuedAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        });

        // 8. Update last login in tenant DB
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await masterDbContext.SaveChangesAsync(cancellationToken);
        await tenantDbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "AUTH_EVENT {EventType} userId={UserId} tenantId={TenantId} ip={IpAddress}",
            "SELECT_COMPANY", claims.UserId, request.TenantId, request.IpAddress);

        var accessTtlMinutes = int.TryParse(configuration["JwtSettings:AccessTokenTtlMinutes"], out var m) ? m : 15;
        return new SelectCompanyResult(accessToken, refreshPlaintext, DateTimeOffset.UtcNow.AddMinutes(accessTtlMinutes));
    }
}
