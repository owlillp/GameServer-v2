using MatchmakingService.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatchmakingService.Core.Services;

public sealed class LeaderboardSynchronizer(
    IServiceScopeFactory scopeFactory,
    LeaderboardSyncSignal signal,
    IOptionsMonitor<MatchmakingOptions> options,
    ILogger<LeaderboardSynchronizer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                var sync = scope.ServiceProvider.GetRequiredService<LeaderboardSync>();

                await sync.SyncAsync(force: false, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Leaderboard cache check failed");
            }

            TimeSpan interval = TimeSpan.FromMinutes(options.CurrentValue.LeaderboardCheckMinutes);

            try
            {
                await signal.WaitAsync(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
