using PhanMemKeToan.Domain.Common;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DatabaseSchemaName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DatabaseMode DatabaseMode { get; set; } = DatabaseMode.CloudManaged;
    public TenantDbStatus DbStatus { get; set; } = TenantDbStatus.Online;
    public string? ConnectionStringEncrypted { get; set; }
    public string? CloudflareSubdomain { get; set; }
    public string? DbHost { get; set; }

    public ICollection<MasterUserTenant> MasterUserTenants { get; set; } = new List<MasterUserTenant>();
}
