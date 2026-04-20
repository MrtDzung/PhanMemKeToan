using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Tenants.Commands.ToggleTenantStatus;

public class ToggleTenantStatusCommandHandler(
    IMasterDbContext masterDbContext
) : IRequestHandler<ToggleTenantStatusCommand>
{
    public async Task Handle(ToggleTenantStatusCommand request, CancellationToken cancellationToken)
    {
        var tenant = await masterDbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Tenant", request.Id);

        tenant.IsActive = request.Activate;

        if (!request.Activate)
        {
            // Revoke all refresh tokens for this tenant
            var activeTokens = await masterDbContext.RefreshTokens
                .Where(rt => rt.TenantId == request.Id && !rt.IsRevoked)
                .ToListAsync(cancellationToken);
            foreach (var token in activeTokens)
                token.IsRevoked = true;
        }

        await masterDbContext.SaveChangesAsync(cancellationToken);
    }
}
