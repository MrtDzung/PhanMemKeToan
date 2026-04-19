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

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    // Lookup entities
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<ExpenseItem> ExpenseItems { get; set; }

    // AccountObject cluster
    public DbSet<AccountObjectGroup> AccountObjectGroups { get; set; }
    public DbSet<AccountObject> AccountObjects { get; set; }
    public DbSet<AccountObjectBankAccount> AccountObjectBankAccounts { get; set; }
    public DbSet<AccountObjectOpeningBalance> AccountObjectOpeningBalances { get; set; }
    public DbSet<AccountObjectEmployeeProfile> AccountObjectEmployeeProfiles { get; set; }

    // InventoryItem cluster
    public DbSet<InventoryItemCategory> InventoryItemCategories { get; set; }
    public DbSet<ItemAttributeType> ItemAttributeTypes { get; set; }
    public DbSet<InventoryQuantityFormulaTemplate> InventoryQuantityFormulaTemplates { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<InventoryItemUnitConvert> InventoryItemUnitConverts { get; set; }
    public DbSet<InventoryQuantityFormulaDetail> InventoryQuantityFormulaDetails { get; set; }
    public DbSet<InventoryItemBarcode> InventoryItemBarcodes { get; set; }
    public DbSet<InventoryItemAttribute> InventoryItemAttributes { get; set; }
    public DbSet<InventoryItemOpeningBalance> InventoryItemOpeningBalances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();

        // Ignore master-scoped entities discovered through navigation chains
        // (Tenant.MasterUserTenants → MasterUserTenant → MasterUser)
        modelBuilder.Ignore<MasterUser>();
        modelBuilder.Ignore<MasterUserTenant>();

        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => !(type.Namespace?.Contains(".Master") ?? false));

        modelBuilder.Entity<User>().HasQueryFilter(
            u => u.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !u.IsDeleted);
        modelBuilder.Entity<Role>().HasQueryFilter(
            r => r.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !r.IsDeleted);
        modelBuilder.Entity<Account>().HasQueryFilter(
            a => a.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !a.IsDeleted);

        // Lookup entity filters
        modelBuilder.Entity<Currency>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<Unit>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<Warehouse>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<Department>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<ExpenseItem>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);

        // AccountObject cluster filters
        modelBuilder.Entity<AccountObjectGroup>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<AccountObject>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<AccountObjectBankAccount>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<AccountObjectOpeningBalance>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<AccountObjectEmployeeProfile>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);

        // InventoryItem cluster filters
        modelBuilder.Entity<InventoryItemCategory>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<ItemAttributeType>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<InventoryQuantityFormulaTemplate>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<InventoryItem>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<InventoryItemUnitConvert>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<InventoryQuantityFormulaDetail>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<InventoryItemBarcode>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<InventoryItemAttribute>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);
        modelBuilder.Entity<InventoryItemOpeningBalance>().HasQueryFilter(e => e.TenantId == (_tenantContext.TenantId ?? Guid.Empty) && !e.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }
}

