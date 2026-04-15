using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Common;
using PhanMemKeToan.Domain.Entities;
using System.Reflection;

namespace PhanMemKeToan.Infrastructure.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantContext tenantContext)
    : DbContext(options), IApplicationDbContext
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<User>().HasQueryFilter(
            u => u.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !u.IsDeleted);
        modelBuilder.Entity<Role>().HasQueryFilter(
            r => r.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !r.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }
}

