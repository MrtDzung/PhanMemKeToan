using Microsoft.Extensions.Caching.Distributed;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Infrastructure.Services;

public class RedisTokenBlacklistService(IDistributedCache cache) : ITokenBlacklistService
{
    private const string KeyPrefix = "blacklist:jti:";

    public async Task BlacklistAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
        await cache.SetStringAsync($"{KeyPrefix}{jti}", "1", options, cancellationToken);
    }

    public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await cache.GetStringAsync($"{KeyPrefix}{jti}", cancellationToken);
            return value is not null;
        }
        catch (Exception)
        {
            // Fail-closed: if Redis is unavailable, treat token as blacklisted
            return true;
        }
    }
}
