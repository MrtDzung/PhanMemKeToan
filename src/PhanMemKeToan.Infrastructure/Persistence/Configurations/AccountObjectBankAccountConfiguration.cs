using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class AccountObjectBankAccountConfiguration : IEntityTypeConfiguration<AccountObjectBankAccount>
{
    public void Configure(EntityTypeBuilder<AccountObjectBankAccount> builder)
    {
        builder.ToTable("AccountObjectBankAccounts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.BankName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.BankBranch).HasMaxLength(255);
        builder.Property(e => e.AccountNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.SwiftCode).HasMaxLength(20);
        builder.HasIndex(e => e.AccountObjectId)
            .HasDatabaseName("IX_AccountObjectBankAccounts_AccountObjectId");
    }
}
