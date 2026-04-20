using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations.Master;

public class MasterUserConfiguration : IEntityTypeConfiguration<MasterUser>
{
    public void Configure(EntityTypeBuilder<MasterUser> builder)
    {
        builder.ToTable("sys_master_users");
        builder.HasKey(mu => mu.Id);
        builder.Property(mu => mu.Email).IsRequired().HasMaxLength(256);
        builder.Property(mu => mu.PasswordHash).IsRequired().HasMaxLength(256);
        builder.Property(mu => mu.FullName).IsRequired().HasMaxLength(200);
        builder.HasIndex(mu => mu.Email).IsUnique();
    }
}
