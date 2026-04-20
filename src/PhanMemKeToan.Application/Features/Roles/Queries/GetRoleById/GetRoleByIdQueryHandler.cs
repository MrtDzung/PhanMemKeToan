using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Roles.Queries.GetRoleById;

public class GetRoleByIdQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetRoleByIdQuery, RoleDetailDto>
{
    public async Task<RoleDetailDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role", request.RoleId);

        return new RoleDetailDto(role.Id, role.Name, role.Description,
            role.RolePermissions.Select(rp => new PermissionRefDto(rp.Permission.Id, rp.Permission.Code, rp.Permission.Name, rp.Permission.ModuleCode)).ToList());
    }
}
