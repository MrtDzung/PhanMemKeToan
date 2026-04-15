using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandHandler(IApplicationDbContext dbContext, ITenantContext tenantContext) : IRequestHandler<CreateRoleCommand, Guid>
{
    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        if (await dbContext.Roles.AnyAsync(r => r.Name == request.Name, cancellationToken))
            throw new InvalidOperationException($"Role '{request.Name}' already exists.");

        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "system"
        };
        dbContext.Roles.Add(role);

        foreach (var permId in request.PermissionIds)
            dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permId });

        await dbContext.SaveChangesAsync(cancellationToken);
        return role.Id;
    }
}
