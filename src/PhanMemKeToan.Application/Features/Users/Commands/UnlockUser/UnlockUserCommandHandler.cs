using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Users.Commands.UnlockUser;

public class UnlockUserCommandHandler(
    IApplicationDbContext dbContext,
    IMasterDbContext masterDbContext
) : IRequestHandler<UnlockUserCommand>
{
    public async Task Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // Reset lockout in Tenant DB
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.ModifiedAt = DateTimeOffset.UtcNow;

        // Reset lockout in Master DB
        var masterUser = await masterDbContext.MasterUsers
            .FirstOrDefaultAsync(mu => mu.Id == request.UserId, cancellationToken);
        if (masterUser is not null)
        {
            masterUser.FailedLoginCount = 0;
            masterUser.LockedUntil = null;
            masterUser.ModifiedAt = DateTimeOffset.UtcNow;
            await masterDbContext.SaveChangesAsync(cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
