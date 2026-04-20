using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class InventoryItemUnitConvertConfiguration : IEntityTypeConfiguration<InventoryItemUnitConvert>
{
    public void Configure(EntityTypeBuilder<InventoryItemUnitConvert> builder)
    {
        builder.ToTable("InventoryItemUnitConverts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ConvertRate).HasPrecision(18, 6);
        builder.HasOne(e => e.InventoryItem)
            .WithMany(i => i.UnitConverts)
            .HasForeignKey(e => e.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Unit)
            .WithMany(u => u.UnitConverts)
            .HasForeignKey(e => e.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.TenantId, e.InventoryItemId, e.UnitId })
            .HasDatabaseName("UIX_InventoryItemUnitConverts_TenantId_ItemId_UnitId").IsUnique()
            .HasFilter("is_deleted = false");
    }
}
