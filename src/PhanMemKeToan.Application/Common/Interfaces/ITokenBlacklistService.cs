namespace PhanMemKeToan.Application.Common.Interfaces;

public interface ITokenBlacklistService
{
    Task BlacklistAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default);
}
