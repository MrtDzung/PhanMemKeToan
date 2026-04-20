using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Roles.Queries.GetRoles;

public class GetRolesQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleListDto>>
{
    public async Task<IReadOnlyList<RoleListDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleListDto(
                r.Id, r.Name, r.Description,
                r.UserRoles.Count(ur => ur.User.IsActive),
                r.RolePermissions.Count))
            .ToListAsync(cancellationToken);
    }
}
