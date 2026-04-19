using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ItemCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.ItemName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.ItemNameEnglish).HasMaxLength(255);
        builder.Property(e => e.Barcode).HasMaxLength(255);
        builder.Property(e => e.DefaultTaxRate).HasPrecision(5, 2);
        builder.Property(e => e.UnitPrice).HasPrecision(18, 2);
        builder.Property(e => e.MinStockLevel).HasPrecision(18, 2);
        builder.Property(e => e.MaxStockLevel).HasPrecision(18, 2);
        builder.Property(e => e.SalePrice1).HasPrecision(18, 2);
        builder.Property(e => e.SalePrice2).HasPrecision(18, 2);
        builder.Property(e => e.SalePrice3).HasPrecision(18, 2);
        builder.Property(e => e.CostingMethod).HasConversion<int>();
        builder.Property(e => e.ItemType).HasConversion<int>();
        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.HasOne(e => e.Unit)
            .WithMany(u => u.Items)
            .HasForeignKey(e => e.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PanelUnit)
            .WithMany()
            .HasForeignKey(e => e.PanelUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.FormulaTemplate)
            .WithMany(t => t.Items)
            .HasForeignKey(e => e.FormulaTemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.TenantId, e.ItemCode })
            .HasDatabaseName("UIX_InventoryItems_TenantId_ItemCode").IsUnique()
            .HasFilter("is_deleted = false");
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_InventoryItems_TenantId");
        builder.HasIndex(e => e.ItemType)
            .HasDatabaseName("IX_InventoryItems_ItemType");
    }
}
