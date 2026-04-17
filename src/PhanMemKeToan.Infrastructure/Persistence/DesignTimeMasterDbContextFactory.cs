using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PhanMemKeToan.Infrastructure.Persistence;

public class DesignTimeMasterDbContextFactory : IDesignTimeDbContextFactory<MasterDbContext>
{
    public MasterDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MasterDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5433;Database=phanmemketoan;Username=postgres;Password=postgres",
            npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(MasterDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention();

        return new MasterDbContext(optionsBuilder.Options);
    }
}
