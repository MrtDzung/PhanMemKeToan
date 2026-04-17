using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AccountNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.AccountName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.AccountNameEnglish)
            .HasMaxLength(128);

        builder.Property(a => a.MISACodeID)
            .HasMaxLength(100);

        builder.Property(a => a.AccountCategoryKind)
            .HasConversion<int>();

        builder.Property(a => a.AccountObjectType)
            .HasConversion<int>();

        // Self-referencing tree
        builder.HasOne(a => a.Parent)
            .WithMany(a => a.Children)
            .HasForeignKey(a => a.ParentID)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique account number per tenant (partial — active accounts only)
        builder.HasIndex(a => new { a.TenantId, a.AccountNumber })
            .HasDatabaseName("UIX_Accounts_TenantId_AccountNumber")
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(a => a.AccountNumber)
            .HasDatabaseName("IX_Accounts_AccountNumber_Pattern");

        builder.HasIndex(a => a.ParentID)
            .HasDatabaseName("IX_Accounts_ParentID");

        builder.HasIndex(a => a.TenantId)
            .HasDatabaseName("IX_Accounts_TenantId");
    }
}
