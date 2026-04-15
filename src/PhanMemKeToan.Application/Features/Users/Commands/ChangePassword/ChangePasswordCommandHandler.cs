using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Common.Exceptions;

namespace PhanMemKeToan.Application.Features.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher
) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new InvalidCredentialsException();

        user.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user.ModifiedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
