using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class AccountObjectOpeningBalanceConfiguration : IEntityTypeConfiguration<AccountObjectOpeningBalance>
{
    public void Configure(EntityTypeBuilder<AccountObjectOpeningBalance> builder)
    {
        builder.ToTable("AccountObjectOpeningBalances");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DebitAmount).HasPrecision(18, 0);
        builder.Property(e => e.CreditAmount).HasPrecision(18, 0);
        builder.Property(e => e.DebitAmountOC).HasPrecision(18, 3);
        builder.Property(e => e.CreditAmountOC).HasPrecision(18, 3);
        builder.Property(e => e.ExchangeRate).HasPrecision(18, 2);

        builder.HasOne(e => e.Currency)
            .WithMany(c => c.OpeningBalances)
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.AccountObjectId, e.CurrencyId })
            .HasDatabaseName("UIX_AccountObjectOpeningBalances_TenantId_ObjectId_CurrencyId")
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
