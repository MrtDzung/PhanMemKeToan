using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Infrastructure.Persistence;

public class TenantDbContextFactory(
    ITenantContext tenantContext,
    ITenantConnectionResolver connectionResolver)
{
    public async Task<ApplicationDbContext> CreateDbContextAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connectionString = await connectionResolver.ResolveConnectionStringAsync(tenantId, cancellationToken);
        tenantContext.TenantId = tenantId;

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            })
            .UseSnakeCaseNamingConvention();

        return new ApplicationDbContext(optionsBuilder.Options, tenantContext);
    }
}
