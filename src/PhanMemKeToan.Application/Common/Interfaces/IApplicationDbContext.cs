using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    // Lookup entities
    DbSet<Currency> Currencies { get; }
    DbSet<Unit> Units { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<Department> Departments { get; }
    DbSet<ExpenseItem> ExpenseItems { get; }

    // AccountObject cluster
    DbSet<AccountObjectGroup> AccountObjectGroups { get; }
    DbSet<AccountObject> AccountObjects { get; }
    DbSet<AccountObjectBankAccount> AccountObjectBankAccounts { get; }
    DbSet<AccountObjectOpeningBalance> AccountObjectOpeningBalances { get; }
    DbSet<AccountObjectEmployeeProfile> AccountObjectEmployeeProfiles { get; }

    // InventoryItem cluster
    DbSet<InventoryItemCategory> InventoryItemCategories { get; }
    DbSet<ItemAttributeType> ItemAttributeTypes { get; }
    DbSet<InventoryQuantityFormulaTemplate> InventoryQuantityFormulaTemplates { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<InventoryItemUnitConvert> InventoryItemUnitConverts { get; }
    DbSet<InventoryQuantityFormulaDetail> InventoryQuantityFormulaDetails { get; }
    DbSet<InventoryItemBarcode> InventoryItemBarcodes { get; }
    DbSet<InventoryItemAttribute> InventoryItemAttributes { get; }
    DbSet<InventoryItemOpeningBalance> InventoryItemOpeningBalances { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
