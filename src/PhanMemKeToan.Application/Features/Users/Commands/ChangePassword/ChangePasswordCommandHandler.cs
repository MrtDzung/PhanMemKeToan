using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Common.Exceptions;

namespace PhanMemKeToan.Application.Features.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IApplicationDbContext dbContext,
    IMasterDbContext masterDbContext,
    IPasswordHasher passwordHasher
) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (string.IsNullOrEmpty(user.PasswordHash) || !passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new InvalidCredentialsException();

        var newHash = passwordHasher.HashPassword(request.NewPassword);

        // Update password in Tenant DB
        user.PasswordHash = newHash;
        user.ModifiedAt = DateTimeOffset.UtcNow;

        // Update password in Master DB
        var masterUser = await masterDbContext.MasterUsers
            .FirstOrDefaultAsync(mu => mu.Id == request.UserId, cancellationToken);
        if (masterUser is not null)
        {
            masterUser.PasswordHash = newHash;
            masterUser.ModifiedAt = DateTimeOffset.UtcNow;

            // Revoke all refresh tokens across all tenants
            var activeTokens = await masterDbContext.RefreshTokens
                .Where(rt => rt.UserId == request.UserId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);
            foreach (var token in activeTokens)
                token.IsRevoked = true;

            await masterDbContext.SaveChangesAsync(cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
