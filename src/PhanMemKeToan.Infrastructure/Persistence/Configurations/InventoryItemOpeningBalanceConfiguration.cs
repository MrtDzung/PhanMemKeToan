using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class InventoryItemOpeningBalanceConfiguration : IEntityTypeConfiguration<InventoryItemOpeningBalance>
{
    public void Configure(EntityTypeBuilder<InventoryItemOpeningBalance> builder)
    {
        builder.ToTable("InventoryItemOpeningBalances");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Quantity).HasPrecision(18, 6);
        builder.Property(e => e.UnitCost).HasPrecision(18, 2);
        builder.Property(e => e.Amount).HasPrecision(18, 0);
        builder.Property(e => e.ForeignAmount).HasPrecision(18, 3);
        builder.Property(e => e.ExchangeRate).HasPrecision(18, 2);
        builder.HasOne(e => e.InventoryItem)
            .WithMany(i => i.OpeningBalances)
            .HasForeignKey(e => e.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Warehouse)
            .WithMany(w => w.OpeningBalances)
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Unit)
            .WithMany(u => u.OpeningBalances)
            .HasForeignKey(e => e.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Currency)
            .WithMany(c => c.InventoryOpeningBalances)
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.TenantId, e.InventoryItemId, e.WarehouseId, e.UnitId })
            .HasDatabaseName("UIX_InventoryItemOpeningBalances_TenantId_ItemId_WarehouseId_UnitId")
            .IsUnique().HasFilter("is_deleted = false");
    }
}
