using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class InventoryItemBarcodeConfiguration : IEntityTypeConfiguration<InventoryItemBarcode>
{
    public void Configure(EntityTypeBuilder<InventoryItemBarcode> builder)
    {
        builder.ToTable("InventoryItemBarcodes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.BarcodeValue).HasMaxLength(255).IsRequired();
        builder.Property(e => e.BarcodeType).HasConversion<int>();
        builder.HasOne(e => e.InventoryItem)
            .WithMany(i => i.Barcodes)
            .HasForeignKey(e => e.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.TenantId, e.BarcodeValue })
            .HasDatabaseName("UIX_InventoryItemBarcodes_TenantId_BarcodeValue").IsUnique()
            .HasFilter("is_deleted = false");
    }
}
