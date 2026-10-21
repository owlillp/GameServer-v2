using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Domain.Leaderboard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatchmakingService.Core.Services;

public sealed class LeaderboardInitializer(
    IServiceScopeFactory scopeFactory,
    IOptions<MatchmakingOptions> options,
    ILogger<LeaderboardInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.LeaderboardRefreshMinutes));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                await RebuildAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to rebuild leaderboard cache");
            }
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    private async Task RebuildAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILeaderboardRepository>();
        var store = scope.ServiceProvider.GetRequiredService<ILeaderboardStore>();

        IReadOnlyList<LeaderboardEntry> entries = await repository.ListAsync(
            options.Value.LeaderboardSize,
            cancellationToken);

        await store.RebuildAsync(entries, cancellationToken);

        logger.LogInformation("Leaderboard cache rebuilt with {Count} entries", entries.Count);
    }

    private static async Task<bool> WaitForNextTickAsync(
        PeriodicTimer timer,
        CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
