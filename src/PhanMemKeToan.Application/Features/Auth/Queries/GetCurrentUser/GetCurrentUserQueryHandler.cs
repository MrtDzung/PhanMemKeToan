using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Common.Exceptions;

namespace PhanMemKeToan.Application.Features.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler(
    IApplicationDbContext dbContext
) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        var tenant = await dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);

        var roles = user.UserRoles
            .Where(ur => !ur.Role.IsDeleted)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        var permissions = user.UserRoles
            .Where(ur => !ur.Role.IsDeleted)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        return new CurrentUserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.IsActive,
            user.TenantId,
            tenant?.Name ?? string.Empty,
            roles,
            permissions,
            user.LastLoginAt,
            user.CreatedAt
        );
    }
}
