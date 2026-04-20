using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class AccountObjectEmployeeProfileConfiguration : IEntityTypeConfiguration<AccountObjectEmployeeProfile>
{
    public void Configure(EntityTypeBuilder<AccountObjectEmployeeProfile> builder)
    {
        builder.ToTable("AccountObjectEmployeeProfiles");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CitizenId).HasMaxLength(20);
        builder.Property(e => e.SocialInsuranceNumber).HasMaxLength(10);

        builder.HasIndex(e => e.AccountObjectId)
            .HasDatabaseName("UIX_AccountObjectEmployeeProfiles_AccountObjectId").IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.CitizenId })
            .HasDatabaseName("UIX_AccountObjectEmployeeProfiles_TenantId_CitizenId").IsUnique()
            .HasFilter("citizen_id IS NOT NULL AND is_deleted = false");

        builder.HasIndex(e => new { e.TenantId, e.SocialInsuranceNumber })
            .HasDatabaseName("UIX_AccountObjectEmployeeProfiles_TenantId_SIN").IsUnique()
            .HasFilter("social_insurance_number IS NOT NULL AND is_deleted = false");

        builder.HasOne(e => e.Department)
            .WithMany(d => d.EmployeeProfiles)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
