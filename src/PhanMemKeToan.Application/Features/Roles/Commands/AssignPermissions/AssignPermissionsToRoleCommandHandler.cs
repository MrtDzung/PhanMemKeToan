using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Roles.Commands.AssignPermissions;

public class AssignPermissionsToRoleCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<AssignPermissionsToRoleCommand>
{
    public async Task Handle(AssignPermissionsToRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role", request.RoleId);

        // Validate permission IDs
        var validPermCount = await dbContext.Permissions
            .CountAsync(p => request.PermissionIds.Contains(p.Id), cancellationToken);
        if (validPermCount != request.PermissionIds.Count)
            throw new NotFoundException("Permission", "one or more permissionIds");

        // Bulk replace
        foreach (var rp in role.RolePermissions.ToList())
            dbContext.RolePermissions.Remove(rp);
        foreach (var permId in request.PermissionIds)
            dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permId });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
