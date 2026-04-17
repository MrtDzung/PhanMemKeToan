using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Enums;
using PhanMemKeToan.Infrastructure.Persistence;

namespace PhanMemKeToan.Infrastructure.Services;

public class TenantConnectionResolver(
    IMasterDbContext masterDbContext,
    IConnectionStringEncryptor encryptor,
    IConfiguration configuration) : ITenantConnectionResolver
{
    public async Task<string> ResolveConnectionStringAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await masterDbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found or inactive");

        if (tenant.DbStatus != TenantDbStatus.Online)
        {
            throw new InvalidOperationException($"Tenant {tenantId} database is {tenant.DbStatus}");
        }

        if (tenant.DatabaseMode == DatabaseMode.OnPremise && !string.IsNullOrEmpty(tenant.ConnectionStringEncrypted))
        {
            return encryptor.Decrypt(tenant.ConnectionStringEncrypted);
        }

        // CloudManaged: construct connection string from template
        var cloudHost = configuration["MultiTenancy:CloudDatabaseHost"]
            ?? configuration.GetConnectionString("DefaultConnection")!;

        // For cloud-managed, all tenants use the same database (shared DB mode for now)
        // Future: per-tenant database with host template
        return cloudHost;
    }
}
