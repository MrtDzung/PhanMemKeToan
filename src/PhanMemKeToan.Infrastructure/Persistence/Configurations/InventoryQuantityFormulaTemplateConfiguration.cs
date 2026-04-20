using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class InventoryQuantityFormulaTemplateConfiguration : IEntityTypeConfiguration<InventoryQuantityFormulaTemplate>
{
    public void Configure(EntityTypeBuilder<InventoryQuantityFormulaTemplate> builder)
    {
        builder.ToTable("InventoryQuantityFormulaTemplates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TemplateCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TemplateName).HasMaxLength(255).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.TemplateCode })
            .HasDatabaseName("UIX_InventoryQuantityFormulaTemplates_TenantId_TemplateCode").IsUnique()
            .HasFilter("is_deleted = false");
    }
}
