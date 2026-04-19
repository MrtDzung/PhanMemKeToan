using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class InventoryItemCategoryConfiguration : IEntityTypeConfiguration<InventoryItemCategory>
{
    public void Configure(EntityTypeBuilder<InventoryItemCategory> builder)
    {
        builder.ToTable("InventoryItemCategories");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CategoryCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.CategoryName).HasMaxLength(255).IsRequired();
        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.TenantId, e.CategoryCode })
            .HasDatabaseName("UIX_InventoryItemCategories_TenantId_CategoryCode").IsUnique()
            .HasFilter("is_deleted = false");
        builder.HasIndex(e => new { e.IsActive, e.SortOrder })
            .HasDatabaseName("IX_InventoryItemCategories_IsActive_SortOrder");
    }
}
