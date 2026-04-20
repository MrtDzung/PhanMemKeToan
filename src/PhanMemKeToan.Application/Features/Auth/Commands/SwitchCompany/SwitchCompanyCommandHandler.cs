using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Common.Exceptions;
using RefreshTokenEntity = PhanMemKeToan.Domain.Entities.RefreshToken;

namespace PhanMemKeToan.Application.Features.Auth.Commands.SwitchCompany;

public class SwitchCompanyCommandHandler(
    IMasterDbContext masterDbContext,
    IApplicationDbContext tenantDbContext,
    IJwtService jwtService,
    IConfiguration configuration,
    ILogger<SwitchCompanyCommandHandler> logger
) : IRequestHandler<SwitchCompanyCommand, SwitchCompanyResult>
{
    public async Task<SwitchCompanyResult> Handle(SwitchCompanyCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify user has access to target tenant
        var access = await masterDbContext.MasterUserTenants
            .AnyAsync(mut => mut.MasterUserId == request.UserId && mut.TenantId == request.TargetTenantId, cancellationToken);

        if (!access)
            throw new ForbiddenAccessException();

        // 2. Verify target tenant is active
        var tenant = await masterDbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TargetTenantId && t.IsActive, cancellationToken)
            ?? throw new NotFoundException("TENANT_NOT_FOUND", $"Tenant {request.TargetTenantId} not found or inactive.");

        // 3. Load user from target Tenant DB
        var user = await tenantDbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.TenantId == request.TargetTenantId, cancellationToken)
            ?? throw new NotFoundException("USER_NOT_FOUND", "User not found in target company.");

        // 4. Build claims
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
        var claimsDto = new UserClaimsDto(user.Id, request.TargetTenantId, user.Email, roles, permissions, jti);

        // 5. Generate new JWT + refresh token
        var accessToken = jwtService.GenerateAccessToken(claimsDto);
        var (refreshPlaintext, refreshHash) = jwtService.GenerateRefreshToken();

        var refreshTtlDays = int.TryParse(configuration["JwtSettings:RefreshTokenTtlDays"], out var d) ? d : 7;
        var refreshExpiry = DateTimeOffset.UtcNow.AddDays(refreshTtlDays);

        masterDbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TenantId = request.TargetTenantId,
            TokenHash = refreshHash,
            TokenFamily = Guid.NewGuid(),
            ExpiresAt = refreshExpiry,
            IssuedAt = DateTimeOffset.UtcNow,
            IsRevoked = false
        });

        await masterDbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "AUTH_EVENT {EventType} userId={UserId} from={FromTenantId} to={ToTenantId} ip={IpAddress}",
            "SWITCH_COMPANY", request.UserId, request.CurrentTenantId, request.TargetTenantId, request.IpAddress);

        var accessTtlMinutes = int.TryParse(configuration["JwtSettings:AccessTokenTtlMinutes"], out var m) ? m : 15;
        return new SwitchCompanyResult(accessToken, refreshPlaintext, DateTimeOffset.UtcNow.AddMinutes(accessTtlMinutes));
    }
}
