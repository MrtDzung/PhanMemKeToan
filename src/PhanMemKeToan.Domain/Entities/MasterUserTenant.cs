namespace PhanMemKeToan.Domain.Entities;

public class MasterUserTenant
{
    public Guid MasterUserId { get; set; }
    public Guid TenantId { get; set; }
    public bool IsDefault { get; set; }
    public string? DisplayRole { get; set; }
    public DateTimeOffset JoinedAt { get; set; }

    public MasterUser MasterUser { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
