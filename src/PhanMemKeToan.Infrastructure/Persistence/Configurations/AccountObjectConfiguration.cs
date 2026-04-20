using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class AccountObjectConfiguration : IEntityTypeConfiguration<AccountObject>
{
    public void Configure(EntityTypeBuilder<AccountObject> builder)
    {
        builder.ToTable("AccountObjects");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ObjectCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.ObjectName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.ObjectNameEnglish).HasMaxLength(255);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.TaxCode).HasMaxLength(50);
        builder.Property(e => e.Email).HasMaxLength(255);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.Fax).HasMaxLength(50);
        builder.Property(e => e.Website).HasMaxLength(255);
        builder.Property(e => e.ContactPerson).HasMaxLength(255);
        builder.Property(e => e.ContactPhone).HasMaxLength(50);
        builder.Property(e => e.CreditLimit).HasPrecision(18, 2);
        builder.Property(e => e.RowVersion).IsConcurrencyToken();

        builder.HasOne(e => e.AccountObjectGroup)
            .WithMany(g => g.AccountObjects)
            .HasForeignKey(e => e.AccountObjectGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.BankAccounts)
            .WithOne(b => b.AccountObject)
            .HasForeignKey(b => b.AccountObjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.OpeningBalances)
            .WithOne(o => o.AccountObject)
            .HasForeignKey(o => o.AccountObjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EmployeeProfile)
            .WithOne(ep => ep.AccountObject)
            .HasForeignKey<AccountObjectEmployeeProfile>(ep => ep.AccountObjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TenantId, e.ObjectCode })
            .HasDatabaseName("UIX_AccountObjects_TenantId_ObjectCode").IsUnique()
            .HasFilter("is_deleted = false");
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_AccountObjects_TenantId");
        builder.HasIndex(e => e.ObjectType)
            .HasDatabaseName("IX_AccountObjects_ObjectType");
    }
}
