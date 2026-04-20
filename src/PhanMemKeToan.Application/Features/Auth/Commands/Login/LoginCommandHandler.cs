using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Common.Models;
using PhanMemKeToan.Domain.Common.Exceptions;

namespace PhanMemKeToan.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler(
    IMasterDbContext masterDbContext,
    IPasswordHasher passwordHasher,
    ITempTokenService tempTokenService,
    IDistributedCache cache,
    ILogger<LoginCommandHandler> logger
) : IRequestHandler<LoginCommand, LoginResult>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var ipKey = $"login:failed:{request.IpAddress}";

        // 1. Check IP-level rate limit
        var counterStr = await cache.GetStringAsync(ipKey, cancellationToken);
        if (int.TryParse(counterStr, out var ipFailCount) && ipFailCount >= MaxFailedAttempts)
        {
            logger.LogWarning("Login rate limited for IP {IpAddress}", request.IpAddress);
            throw new AccountLockedException(DateTimeOffset.UtcNow.Add(RateLimitWindow));
        }

        // 2. Load MasterUser (credentials are in Master DB now)
        var masterUser = await masterDbContext.MasterUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(mu => mu.Email == request.Email, cancellationToken);

        // 3. Check account deactivation BEFORE password verification
        if (masterUser is not null && !masterUser.IsActive)
        {
            logger.LogInformation("Login attempt for deactivated account {Email}", request.Email);
            throw new AccountDeactivatedException();
        }

        // 4. Check per-user lockout
        if (masterUser is not null && masterUser.LockedUntil.HasValue && masterUser.LockedUntil > DateTimeOffset.UtcNow)
        {
            throw new AccountLockedException(masterUser.LockedUntil.Value);
        }

        // 5. Verify password
        var passwordValid = masterUser is not null && passwordHasher.VerifyPassword(request.Password, masterUser.PasswordHash);

        if (!passwordValid)
        {
            var newCount = (ipFailCount + 1).ToString();
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = RateLimitWindow };
            await cache.SetStringAsync(ipKey, newCount, options, cancellationToken);

            if (masterUser is not null)
            {
                // Update lockout counter in Master DB (need tracking)
                var tracked = await masterDbContext.MasterUsers
                    .FirstAsync(mu => mu.Id == masterUser.Id, cancellationToken);
                tracked.FailedLoginCount++;
                if (tracked.FailedLoginCount >= MaxFailedAttempts)
                    tracked.LockedUntil = DateTimeOffset.UtcNow.Add(LockDuration);
                await masterDbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogWarning("Failed login for {Email} from {IpAddress}", request.Email, request.IpAddress);
            throw new InvalidCredentialsException();
        }

        // 6. Reset counters on success
        var userToUpdate = await masterDbContext.MasterUsers
            .FirstAsync(mu => mu.Id == masterUser!.Id, cancellationToken);
        userToUpdate.FailedLoginCount = 0;
        userToUpdate.LockedUntil = null;
        await masterDbContext.SaveChangesAsync(cancellationToken);

        // 7. Load accessible companies
        var companies = await masterDbContext.MasterUserTenants
            .Where(mut => mut.MasterUserId == masterUser!.Id)
            .Join(masterDbContext.Tenants.Where(t => t.IsActive),
                mut => mut.TenantId, t => t.Id,
                (mut, t) => new CompanyInfo(
                    t.Id, t.Name, t.Code, t.DatabaseMode, t.DbStatus,
                    mut.DisplayRole, mut.IsDefault))
            .ToListAsync(cancellationToken);

        // 8. Generate tempToken (HMAC-SHA256, 60s TTL)
        var tempToken = tempTokenService.Generate(
            new TempTokenClaims(masterUser!.Id, request.RememberMe));

        logger.LogInformation(
            "AUTH_EVENT {EventType} userId={UserId} ip={IpAddress} companies={CompanyCount}",
            "LOGIN_STEP1", masterUser.Id, request.IpAddress, companies.Count);

        return new LoginResult(tempToken, companies, request.RememberMe);
    }
}
