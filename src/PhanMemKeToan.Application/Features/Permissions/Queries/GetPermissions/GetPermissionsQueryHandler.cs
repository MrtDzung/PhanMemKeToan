using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Permissions.Queries.GetPermissions;

public class GetPermissionsQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionGroupDto>>
{
    public async Task<IReadOnlyList<PermissionGroupDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var all = await dbContext.Permissions
            .OrderBy(p => p.ModuleCode).ThenBy(p => p.Code)
            .ToListAsync(cancellationToken);

        return all
            .GroupBy(p => p.ModuleCode)
            .Select(g => new PermissionGroupDto(g.Key,
                g.Select(p => new PermissionItemDto(p.Id, p.Code, p.Name)).ToList()))
            .ToList();
    }
}
