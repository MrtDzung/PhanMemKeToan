using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; set; }
}
