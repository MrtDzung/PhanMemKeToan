using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Tenants.Commands.GrantTenantAccess;

public class GrantTenantAccessCommandHandler(
    IMasterDbContext masterDbContext
) : IRequestHandler<GrantTenantAccessCommand>
{
    public async Task Handle(GrantTenantAccessCommand request, CancellationToken cancellationToken)
    {
        var tenantExists = await masterDbContext.Tenants
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken);
        if (!tenantExists)
            throw new NotFoundException("Tenant", request.TenantId);

        var userExists = await masterDbContext.MasterUsers
            .AnyAsync(u => u.Id == request.MasterUserId, cancellationToken);
        if (!userExists)
            throw new NotFoundException("MasterUser", request.MasterUserId);

        var alreadyGranted = await masterDbContext.MasterUserTenants
            .AnyAsync(ut => ut.TenantId == request.TenantId && ut.MasterUserId == request.MasterUserId, cancellationToken);
        if (alreadyGranted)
            throw new InvalidOperationException("User already has access to this tenant.");

        // If new access is set as default, unset current default for this user
        if (request.IsDefault)
        {
            var currentDefault = await masterDbContext.MasterUserTenants
                .Where(ut => ut.MasterUserId == request.MasterUserId && ut.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var item in currentDefault)
                item.IsDefault = false;
        }

        masterDbContext.MasterUserTenants.Add(new MasterUserTenant
        {
            TenantId = request.TenantId,
            MasterUserId = request.MasterUserId,
            IsDefault = request.IsDefault,
            DisplayRole = request.DisplayRole,
            JoinedAt = DateTimeOffset.UtcNow
        });

        await masterDbContext.SaveChangesAsync(cancellationToken);
    }
}
