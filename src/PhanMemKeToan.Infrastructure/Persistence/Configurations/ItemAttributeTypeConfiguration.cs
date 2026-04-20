using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class ItemAttributeTypeConfiguration : IEntityTypeConfiguration<ItemAttributeType>
{
    public void Configure(EntityTypeBuilder<ItemAttributeType> builder)
    {
        builder.ToTable("ItemAttributeTypes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.AttributeCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.AttributeName).HasMaxLength(255).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.AttributeCode })
            .HasDatabaseName("UIX_ItemAttributeTypes_TenantId_AttributeCode").IsUnique()
            .HasFilter("is_deleted = false");
    }
}
