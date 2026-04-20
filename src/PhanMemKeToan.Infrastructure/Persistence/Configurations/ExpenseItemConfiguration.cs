using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class ExpenseItemConfiguration : IEntityTypeConfiguration<ExpenseItem>
{
    public void Configure(EntityTypeBuilder<ExpenseItem> builder)
    {
        builder.ToTable("ExpenseItems");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ExpenseCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.ExpenseName).HasMaxLength(255).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.ExpenseCode })
            .HasDatabaseName("UIX_ExpenseItems_TenantId_ExpenseCode").IsUnique()
            .HasFilter("is_deleted = false");
    }
}
