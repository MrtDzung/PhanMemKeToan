using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Permissions.Queries.GetPermissionMatrix;

public class GetPermissionMatrixQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetPermissionMatrixQuery, PermissionMatrixDto>
{
    public async Task<PermissionMatrixDto> Handle(GetPermissionMatrixQuery request, CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles.OrderBy(r => r.Name).ToListAsync(cancellationToken);
        var permissions = await dbContext.Permissions.OrderBy(p => p.ModuleCode).ThenBy(p => p.Code).ToListAsync(cancellationToken);
        var rolePermissions = await dbContext.RolePermissions.ToListAsync(cancellationToken);

        var roleSet = new HashSet<(Guid RoleId, Guid PermId)>(rolePermissions.Select(rp => (rp.RoleId, rp.PermissionId)));

        var roleColumns = roles.Select(r => new RoleColumnDto(r.Id, r.Name)).ToList();

        var modules = permissions
            .GroupBy(p => p.ModuleCode)
            .Select(g => new PermissionModuleDto(g.Key,
                g.Select(p => new PermissionRowDto(p.Id, p.Code, p.Name,
                    roles.ToDictionary(r => r.Id, r => roleSet.Contains((r.Id, p.Id)))))
                .ToList()))
            .ToList();

        return new PermissionMatrixDto(roleColumns, modules);
    }
}
