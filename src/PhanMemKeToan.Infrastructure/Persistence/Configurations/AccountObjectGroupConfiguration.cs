using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class AccountObjectGroupConfiguration : IEntityTypeConfiguration<AccountObjectGroup>
{
    public void Configure(EntityTypeBuilder<AccountObjectGroup> builder)
    {
        builder.ToTable("AccountObjectGroups");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.GroupCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.GroupName).HasMaxLength(255).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.GroupCode })
            .HasDatabaseName("UIX_AccountObjectGroups_TenantId_GroupCode").IsUnique()
            .HasFilter("is_deleted = false");
    }
}
