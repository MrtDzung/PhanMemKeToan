using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Users.Commands.ToggleUserActivation;

public class ToggleUserActivationCommandHandler(
    IApplicationDbContext dbContext,
    IMasterDbContext masterDbContext,
    ITenantContext tenantContext
) : IRequestHandler<ToggleUserActivationCommand>
{
    public async Task Handle(ToggleUserActivationCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (!request.Activate && !user.IsActive)
            throw new InvalidOperationException("ALREADY_DEACTIVATED");

        user.IsActive = request.Activate;
        user.ModifiedAt = DateTimeOffset.UtcNow;

        // On deactivation, revoke refresh tokens for this user in the current tenant (Master DB)
        if (!request.Activate)
        {
            var tenantId = tenantContext.TenantId;
            var activeTokens = await masterDbContext.RefreshTokens
                .Where(rt => rt.UserId == request.UserId && rt.TenantId == tenantId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);
            foreach (var token in activeTokens)
                token.IsRevoked = true;

            await masterDbContext.SaveChangesAsync(cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
