using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Infrastructure.Persistence;

namespace PhanMemKeToan.Infrastructure.Services;

public class TenantRepository(ApplicationDbContext dbContext, IDistributedCache cache) : ITenantRepository
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<TenantDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant:code:{code}";
        return await GetCachedOrFetchAsync(cacheKey,
            () => dbContext.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.Code == code)
                .Select(t => new TenantDto(t.Id, t.Code, t.Name, t.IsActive))
                .FirstOrDefaultAsync(cancellationToken),
            cancellationToken);
    }

    public async Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant:id:{id}";
        return await GetCachedOrFetchAsync(cacheKey,
            () => dbContext.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.Id == id)
                .Select(t => new TenantDto(t.Id, t.Code, t.Name, t.IsActive))
                .FirstOrDefaultAsync(cancellationToken),
            cancellationToken);
    }

    private async Task<TenantDto?> GetCachedOrFetchAsync(
        string cacheKey,
        Func<Task<TenantDto?>> fetch,
        CancellationToken cancellationToken)
    {
        try
        {
            var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
            if (cached is not null)
                return JsonSerializer.Deserialize<TenantDto>(cached);
        }
        catch { /* Redis unavailable — fall through to DB */ }

        var result = await fetch();

        if (result is not null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl };
                await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), options, cancellationToken);
            }
            catch { /* Redis unavailable — ignore */ }
        }

        return result;
    }
}
