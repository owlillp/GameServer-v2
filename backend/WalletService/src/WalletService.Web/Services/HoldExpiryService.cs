using Microsoft.EntityFrameworkCore;
using WalletService.Core.Services;
using WalletService.Domain.Wallets.Holds;
using WalletService.Infrastructure.Postgres;

namespace WalletService.Web.Services;

public sealed class HoldExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<HoldExpiryService> logger) : BackgroundService
{
    private static readonly TimeSpan SWEEP_INTERVAL = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SWEEP_INTERVAL);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }

                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Expired hold sweep failed");
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WalletServiceDbContext>();
        var walletService = scope.ServiceProvider.GetRequiredService<IWalletService>();

        DateTime now = DateTime.UtcNow;
        List<Guid> expiredHoldIds = await dbContext.Holds
            .Where(hold => hold.Status == HoldStatus.HELD && hold.ExpiresAt <= now)
            .Select(hold => hold.Id)
            .Take(200)
            .ToListAsync(cancellationToken);

        foreach (Guid holdId in expiredHoldIds)
        {
            var releaseResult = await walletService.ReleaseAsync(holdId, cancellationToken);
            if (releaseResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to release expired hold {HoldId}: {Error}",
                    holdId,
                    releaseResult.Error.ToString());
                continue;
            }

            logger.LogInformation("Expired hold {HoldId} released", holdId);
        }
    }
}
