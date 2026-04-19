using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(e => e.CurrencyName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.CurrencyNameEnglish).HasMaxLength(100);
        builder.Property(e => e.Symbol).HasMaxLength(10).IsRequired();
        builder.Property(e => e.ExchangeRate).HasPrecision(18, 2);
        builder.HasIndex(e => new { e.TenantId, e.CurrencyCode })
            .HasDatabaseName("UIX_Currencies_TenantId_CurrencyCode").IsUnique()
            .HasFilter("is_deleted = false");
    }
}
