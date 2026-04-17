using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Common;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence;

public class MasterDbContext(DbContextOptions<MasterDbContext> options)
    : DbContext(options), IMasterDbContext
{
    public DbSet<MasterUser> MasterUsers { get; set; }
    public DbSet<MasterUserTenant> MasterUserTenants { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();

        // Ignore tenant-scoped entities discovered through navigation chains
        // (RefreshToken.User → User → UserRole → Role → RolePermission)
        modelBuilder.Ignore<User>();
        modelBuilder.Ignore<Role>();
        modelBuilder.Ignore<Permission>();
        modelBuilder.Ignore<UserRole>();
        modelBuilder.Ignore<RolePermission>();

        modelBuilder.ApplyConfiguration(new Configurations.Master.MasterUserConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.Master.MasterUserTenantConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.Master.MasterTenantConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.Master.MasterRefreshTokenConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
