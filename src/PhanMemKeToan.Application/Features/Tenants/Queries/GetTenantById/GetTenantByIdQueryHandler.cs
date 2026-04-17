using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Application.Features.Tenants.Queries.GetTenantById;

public class GetTenantByIdQueryHandler(
    IMasterDbContext masterDbContext
) : IRequestHandler<GetTenantByIdQuery, TenantDetailDto>
{
    public async Task<TenantDetailDto> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await masterDbContext.Tenants
            .AsNoTracking()
            .Include(t => t.MasterUserTenants)
                .ThenInclude(mut => mut.MasterUser)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Tenant", request.Id);

        var users = tenant.MasterUserTenants
            .Select(mut => new TenantUserDto(
                mut.MasterUserId,
                mut.MasterUser.Email,
                mut.MasterUser.FullName,
                mut.DisplayRole,
                mut.IsDefault,
                mut.JoinedAt))
            .OrderBy(u => u.Email)
            .ToList();

        return new TenantDetailDto(
            tenant.Id,
            tenant.Code,
            tenant.Name,
            tenant.IsActive,
            tenant.DatabaseMode,
            tenant.DbStatus,
            tenant.DatabaseSchemaName,
            tenant.CloudflareSubdomain,
            tenant.DbHost,
            !string.IsNullOrEmpty(tenant.ConnectionStringEncrypted),
            users.Count,
            tenant.CreatedAt,
            users);
    }
}
