namespace PhanMemKeToan.Application.Common.Interfaces;

public interface ITenantConnectionResolver
{
    Task<string> ResolveConnectionStringAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
