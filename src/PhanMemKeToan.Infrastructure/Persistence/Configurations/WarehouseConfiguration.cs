using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.WarehouseCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.WarehouseName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.HasIndex(e => new { e.TenantId, e.WarehouseCode })
            .HasDatabaseName("UIX_Warehouses_TenantId_WarehouseCode").IsUnique()
            .HasFilter("is_deleted = false");
    }
}
