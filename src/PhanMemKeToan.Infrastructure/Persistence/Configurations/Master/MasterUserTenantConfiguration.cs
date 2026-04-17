using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations.Master;

public class MasterUserTenantConfiguration : IEntityTypeConfiguration<MasterUserTenant>
{
    public void Configure(EntityTypeBuilder<MasterUserTenant> builder)
    {
        builder.ToTable("sys_master_user_tenants");
        builder.HasKey(mut => new { mut.MasterUserId, mut.TenantId });

        builder.HasOne(mut => mut.MasterUser)
            .WithMany(mu => mu.MasterUserTenants)
            .HasForeignKey(mut => mut.MasterUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mut => mut.Tenant)
            .WithMany(t => t.MasterUserTenants)
            .HasForeignKey(mut => mut.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mut => mut.TenantId)
            .HasDatabaseName("ix_sys_master_user_tenants_tenant_id");
    }
}
