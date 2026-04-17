using PhanMemKeToan.Application.Features.Accounts.DTOs;

namespace PhanMemKeToan.Application.Common.Interfaces;

public interface IAccountCacheService
{
    Task<List<AccountTreeNodeDto>?> GetTreeAsync(Guid tenantId, bool includeInactive);
    Task SetTreeAsync(Guid tenantId, bool includeInactive, List<AccountTreeNodeDto> data, TimeSpan ttl);
    Task InvalidateTreeAsync(Guid tenantId);
}
