using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UnitCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.UnitName).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.UnitCode })
            .HasDatabaseName("UIX_Units_TenantId_UnitCode").IsUnique()
            .HasFilter("is_deleted = false");
    }
}
