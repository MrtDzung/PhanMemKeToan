using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PhanMemKeToan.Application.Common.Interfaces;

namespace PhanMemKeToan.Infrastructure.Services;

public class RedisTokenBlacklistService(IDistributedCache cache, ILogger<RedisTokenBlacklistService> logger) : ITokenBlacklistService
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
        catch (Exception ex)
        {
            // Fail-open: if Redis is unavailable, allow the token (log warning for ops visibility)
            // Fail-closed would block ALL authenticated requests when Redis is down, which is worse
            logger.LogWarning(ex, "Redis unavailable during token blacklist check for jti={Jti}. Treating as NOT blacklisted (fail-open).", jti);
            return false;
        }
    }
}
