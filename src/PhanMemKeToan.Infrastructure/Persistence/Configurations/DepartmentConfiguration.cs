using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DepartmentCode).HasMaxLength(25).IsRequired();
        builder.Property(e => e.DepartmentName).HasMaxLength(255).IsRequired();
        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.TenantId, e.DepartmentCode })
            .HasDatabaseName("UIX_Departments_TenantId_DepartmentCode").IsUnique()
            .HasFilter("is_deleted = false");
    }
}
