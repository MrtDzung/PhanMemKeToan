using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Common.Exceptions;

namespace PhanMemKeToan.Application.Features.Roles.Commands.DeleteRole;

public class DeleteRoleCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<DeleteRoleCommand>
{
    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .Include(r => r.UserRoles).ThenInclude(ur => ur.User)
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role", request.RoleId);

        // Check no active user assignments
        var activeUsers = role.UserRoles.Where(ur => ur.User.IsActive).Select(ur => ur.User.FullName).ToList();
        if (activeUsers.Any())
            throw new RoleInUseException(activeUsers);

        // Soft-delete role, hard-delete RolePermission join rows
        role.IsDeleted = true;
        role.ModifiedAt = DateTimeOffset.UtcNow;
        foreach (var rp in role.RolePermissions.ToList())
            dbContext.RolePermissions.Remove(rp);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
