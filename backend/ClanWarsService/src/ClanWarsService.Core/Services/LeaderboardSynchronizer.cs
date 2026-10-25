using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClanWarsService.Core.Services;

/// <summary>
/// Checks the current season's ZSET on a timer, and any season a request path flagged.
/// </summary>
public sealed class LeaderboardSynchronizer(
    IServiceScopeFactory scopeFactory,
    LeaderboardSyncSignal signal,
    IOptionsMonitor<ClanWarsOptions> options,
    ILogger<LeaderboardSynchronizer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Guid? flagged = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                var sync = scope.ServiceProvider.GetRequiredService<LeaderboardSync>();
                var seasons = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();

                Guid? seasonId = flagged ?? (await seasons.GetCurrentAsync(stoppingToken))?.Id;
                if (seasonId is not null)
                {
                    await sync.SyncAsync(seasonId.Value, force: false, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Clan wars leaderboard check failed");
            }

            try
            {
                flagged = await signal.WaitAsync(TimeSpan.FromMinutes(options.CurrentValue.LeaderboardCheckMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
