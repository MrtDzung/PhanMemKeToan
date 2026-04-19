using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class InventoryQuantityFormulaDetailConfiguration : IEntityTypeConfiguration<InventoryQuantityFormulaDetail>
{
    public void Configure(EntityTypeBuilder<InventoryQuantityFormulaDetail> builder)
    {
        builder.ToTable("InventoryQuantityFormulaDetails");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Quantity).HasPrecision(18, 6);
        builder.HasOne(e => e.FormulaTemplate)
            .WithMany(t => t.Details)
            .HasForeignKey(e => e.FormulaTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.MaterialItem)
            .WithMany()
            .HasForeignKey(e => e.MaterialItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Unit)
            .WithMany(u => u.FormulaDetails)
            .HasForeignKey(e => e.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
