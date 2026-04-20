using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Infrastructure.Persistence;

public class DesignTimeApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5433;Database=phanmemketoan;Username=postgres;Password=postgres",
            npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention();

        var tenantContext = new DesignTimeTenantContext();
        return new ApplicationDbContext(optionsBuilder.Options, tenantContext);
    }

    private class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; } = Guid.Empty;
    }
}
