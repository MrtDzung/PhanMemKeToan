using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Common.Exceptions;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler(
    IApplicationDbContext dbContext
) : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // Re-validate email uniqueness if changed
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            var emailExists = await dbContext.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != request.UserId, cancellationToken);
            if (emailExists)
                throw new DuplicateEmailException(request.Email);
            user.Email = request.Email;
        }

        user.FullName = request.FullName;
        user.ModifiedAt = DateTimeOffset.UtcNow;

        // Replace roles atomically
        var existingRoles = user.UserRoles.ToList();
        foreach (var ur in existingRoles)
            dbContext.UserRoles.Remove(ur);

        foreach (var roleId in request.RoleIds)
            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
