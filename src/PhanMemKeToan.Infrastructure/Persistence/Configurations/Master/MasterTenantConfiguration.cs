using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations.Master;

public class MasterTenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("sys_tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.DatabaseSchemaName).HasMaxLength(100);
        builder.Property(t => t.ConnectionStringEncrypted).HasMaxLength(2000);
        builder.Property(t => t.CloudflareSubdomain).HasMaxLength(200);
        builder.Property(t => t.DbHost).HasMaxLength(500);
        builder.Property(t => t.DatabaseMode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(DatabaseMode.CloudManaged);
        builder.Property(t => t.DbStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TenantDbStatus.Online);
        builder.HasIndex(t => t.Code).IsUnique();
    }
}
