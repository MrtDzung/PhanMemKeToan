using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Common.Models;

namespace PhanMemKeToan.Application.Features.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler(
    IApplicationDbContext dbContext,
    IMasterDbContext masterDbContext
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

        var tenant = await masterDbContext.Tenants
            .AsNoTracking()
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

        // Load all accessible companies from Master DB
        var companies = await masterDbContext.MasterUserTenants
            .Where(mut => mut.MasterUserId == request.UserId)
            .Join(masterDbContext.Tenants.Where(t => t.IsActive),
                mut => mut.TenantId, t => t.Id,
                (mut, t) => new CompanyInfo(
                    t.Id, t.Name, t.Code, t.DatabaseMode, t.DbStatus,
                    mut.DisplayRole, mut.IsDefault))
            .ToListAsync(cancellationToken);

        var currentCompany = companies.FirstOrDefault(c => c.TenantId == user.TenantId);

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
            user.CreatedAt,
            companies,
            currentCompany
        );
    }
}
