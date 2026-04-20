namespace PhanMemKeToan.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; set; }
}
