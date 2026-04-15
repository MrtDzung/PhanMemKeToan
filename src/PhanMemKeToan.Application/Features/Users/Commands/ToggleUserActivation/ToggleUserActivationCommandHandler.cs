using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Users.Commands.ToggleUserActivation;

public class ToggleUserActivationCommandHandler(
    IApplicationDbContext dbContext
) : IRequestHandler<ToggleUserActivationCommand>
{
    public async Task Handle(ToggleUserActivationCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (!request.Activate && !user.IsActive)
            throw new InvalidOperationException("ALREADY_DEACTIVATED");

        user.IsActive = request.Activate;
        user.ModifiedAt = DateTimeOffset.UtcNow;

        // On deactivation, revoke all refresh tokens
        if (!request.Activate)
        {
            foreach (var token in user.RefreshTokens.Where(t => !t.IsRevoked))
                token.IsRevoked = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
