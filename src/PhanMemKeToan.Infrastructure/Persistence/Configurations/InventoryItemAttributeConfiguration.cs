using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class InventoryItemAttributeConfiguration : IEntityTypeConfiguration<InventoryItemAttribute>
{
    public void Configure(EntityTypeBuilder<InventoryItemAttribute> builder)
    {
        builder.ToTable("InventoryItemAttributes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.AttributeValue).HasMaxLength(500).IsRequired();
        builder.HasOne(e => e.InventoryItem)
            .WithMany(i => i.ItemAttributes)
            .HasForeignKey(e => e.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.AttributeType)
            .WithMany(at => at.ItemAttributes)
            .HasForeignKey(e => e.AttributeTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.TenantId, e.InventoryItemId, e.AttributeTypeId })
            .HasDatabaseName("UIX_InventoryItemAttributes_TenantId_ItemId_AttributeTypeId").IsUnique()
            .HasFilter("is_deleted = false");
    }
}
