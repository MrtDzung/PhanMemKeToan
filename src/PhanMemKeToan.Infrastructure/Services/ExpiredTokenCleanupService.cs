using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PhanMemKeToan.Infrastructure.Services;

public sealed class ExpiredTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiredTokenCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ExpiredTokenCleanupService started. Cleanup interval: {Interval}h", CleanupInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CleanupInterval, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await CleanupExpiredTokensAsync(stoppingToken);
        }

        logger.LogInformation("ExpiredTokenCleanupService stopping.");
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PhanMemKeToan.Infrastructure.Persistence.ApplicationDbContext>();

            var cutoff = DateTimeOffset.UtcNow;

            var deleted = await dbContext.RefreshTokens
                .Where(t => t.ExpiresAt < cutoff || t.IsRevoked)
                .ExecuteDeleteAsync(cancellationToken);

            if (deleted > 0)
                logger.LogInformation("ExpiredTokenCleanupService: deleted {Count} expired/revoked refresh tokens.", deleted);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ExpiredTokenCleanupService: error during cleanup.");
        }
    }
}