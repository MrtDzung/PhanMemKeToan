namespace PhanMemKeToan.Domain.Common;

public abstract class AuditableEntity : BaseEntity, IAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
}
