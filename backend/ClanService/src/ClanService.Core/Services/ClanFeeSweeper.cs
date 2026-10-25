using ClanService.Core.Abstractions;
using ClanService.Core.Configuration;
using ClanService.Domain.Clans;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Database;
using WalletService.Contracts.Communication;

namespace ClanService.Core.Services;

/// <summary>
/// Commits creation-fee holds that were not committed right after the clan was created
/// (wallet unavailable, crash between the two steps).
/// </summary>
public sealed class ClanFeeSweeper(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<ClanOptions> options,
    TimeProvider timeProvider,
    ILogger<ClanFeeSweeper> logger) : BackgroundService
{
    private const int BATCH_SIZE = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(options.CurrentValue.FeeSweepIntervalSeconds),
                    timeProvider,
                    stoppingToken);

                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Clan fee sweep failed");
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var clanRepository = scope.ServiceProvider.GetRequiredService<IClanRepository>();
        var walletClient = scope.ServiceProvider.GetRequiredService<IWalletClient>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        await SkipLockedBatch.RunAsync(
            transactionManager,
            token => clanRepository.ClaimFeePendingAsync(BATCH_SIZE, token),
            async (clans, token) =>
            {
                foreach (Clan clan in clans)
                {
                    var commitResult = await walletClient.CommitAsync(clan.FeeHoldId!.Value, token);
                    if (commitResult.IsSuccess)
                    {
                        clan.MarkFeeCommitted(timeProvider.GetUtcNow().UtcDateTime);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Creation fee of clan {ClanId} is still not committed: {Error}",
                            clan.Id,
                            commitResult.Error.GetMessage());
                    }
                }

                (await transactionManager.SaveChangesAsync(token)).ThrowIfFailed();
            },
            cancellationToken);
    }
}
