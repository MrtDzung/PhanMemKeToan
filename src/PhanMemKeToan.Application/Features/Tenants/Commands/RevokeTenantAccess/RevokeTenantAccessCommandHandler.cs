using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Tenants.Commands.RevokeTenantAccess;

public class RevokeTenantAccessCommandHandler(
    IMasterDbContext masterDbContext
) : IRequestHandler<RevokeTenantAccessCommand>
{
    public async Task Handle(RevokeTenantAccessCommand request, CancellationToken cancellationToken)
    {
        var entry = await masterDbContext.MasterUserTenants
            .FirstOrDefaultAsync(ut => ut.TenantId == request.TenantId && ut.MasterUserId == request.MasterUserId, cancellationToken)
            ?? throw new NotFoundException("TenantAccess", $"{request.TenantId}/{request.MasterUserId}");

        masterDbContext.MasterUserTenants.Remove(entry);

        // Revoke any refresh tokens this user has for this tenant
        var tokens = await masterDbContext.RefreshTokens
            .Where(rt => rt.UserId == request.MasterUserId && rt.TenantId == request.TenantId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.IsRevoked = true;

        await masterDbContext.SaveChangesAsync(cancellationToken);
    }
}
