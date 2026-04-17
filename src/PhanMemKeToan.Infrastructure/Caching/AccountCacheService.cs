using Microsoft.Extensions.Caching.Distributed;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Accounts.DTOs;
using System.Text.Json;

namespace PhanMemKeToan.Infrastructure.Caching;

public class AccountCacheService(IDistributedCache cache) : IAccountCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string GetKey(Guid tenantId, bool includeInactive) =>
        $"accounts:tree:{tenantId}:{(includeInactive ? "1" : "0")}";

    public async Task<List<AccountTreeNodeDto>?> GetTreeAsync(Guid tenantId, bool includeInactive)
    {
        var key = GetKey(tenantId, includeInactive);
        var json = await cache.GetStringAsync(key);
        if (json == null) return null;
        return JsonSerializer.Deserialize<List<AccountTreeNodeDto>>(json, JsonOptions);
    }

    public async Task SetTreeAsync(Guid tenantId, bool includeInactive, List<AccountTreeNodeDto> data, TimeSpan ttl)
    {
        var key = GetKey(tenantId, includeInactive);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });
    }

    public async Task InvalidateTreeAsync(Guid tenantId)
    {
        await cache.RemoveAsync(GetKey(tenantId, false));
        await cache.RemoveAsync(GetKey(tenantId, true));
    }
}
