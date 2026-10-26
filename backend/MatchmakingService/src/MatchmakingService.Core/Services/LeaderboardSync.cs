using System.Threading.Channels;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Domain.Leaderboard;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatchmakingService.Core.Services;

public enum LeaderboardSyncOutcome
{
    InSync = 1,
    Rebuilt = 2,
    LockedByAnotherReplica = 3,
}

/// <summary>
/// Keeps the Redis leaderboard equal to PostgreSQL. PostgreSQL is the source of truth;
/// the cache is rebuilt only when it diverges, and only by the replica holding the advisory lock.
/// </summary>
public sealed class LeaderboardSync(
    ILeaderboardRepository repository,
    ILeaderboardStore store,
    IOptionsMonitor<MatchmakingOptions> options,
    ILogger<LeaderboardSync> logger)
{
    public async Task<LeaderboardSyncOutcome> SyncAsync(bool force, CancellationToken cancellationToken)
    {
        if (!force)
        {
            if (!await HasDivergedAsync(cancellationToken))
            {
                return LeaderboardSyncOutcome.InSync;
            }

            MatchmakingMetrics.LeaderboardDivergences.Add(1);
        }

        await using IAsyncDisposable? rebuildLock = await repository.TryAcquireRebuildLockAsync(cancellationToken);
        if (rebuildLock is null)
        {
            return LeaderboardSyncOutcome.LockedByAnotherReplica;
        }

        int batchSize = options.CurrentValue.LeaderboardRebuildBatchSize;

        await store.ReplaceAsync(
            (afterAccountId, token) => repository.ListAfterAsync(afterAccountId, batchSize, token),
            cancellationToken);

        logger.LogInformation("Leaderboard cache rebuilt from PostgreSQL");

        return LeaderboardSyncOutcome.Rebuilt;
    }

    private async Task<bool> HasDivergedAsync(CancellationToken cancellationToken)
    {
        int databaseCount = await repository.CountAsync(cancellationToken);
        long cacheCount = await store.CountAsync(cancellationToken);

        if (databaseCount != cacheCount)
        {
            logger.LogWarning(
                "Leaderboard cache has {CacheCount} entries, PostgreSQL has {DatabaseCount}",
                cacheCount,
                databaseCount);

            return true;
        }

        int sampleSize = options.CurrentValue.LeaderboardCheckSampleSize;
        IReadOnlyList<LeaderboardEntry> databaseTop = await repository.GetTopAsync(0, sampleSize, cancellationToken);
        IReadOnlyList<LeaderboardRow> cacheTop = await store.GetTopAsync(sampleSize, cancellationToken);

        if (databaseTop.Count != cacheTop.Count)
        {
            return true;
        }

        for (int index = 0; index < databaseTop.Count; index++)
        {
            LeaderboardEntry expected = databaseTop[index];
            LeaderboardRow actual = cacheTop[index];

            if (expected.AccountId != actual.AccountId
                || LeaderboardScore.Encode(expected.Points, expected.UpdatedAt).CompareTo(actual.Score) != 0)
            {
                logger.LogWarning("Leaderboard cache differs from PostgreSQL at rank {Rank}", index + 1);
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Lets request paths ask for an early divergence check, e.g. after a failed Redis write.
/// </summary>
public sealed class LeaderboardSyncSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
    });

    public void Request() => _channel.Writer.TryWrite(true);

    public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            await _channel.Reader.ReadAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Regular interval elapsed.
        }
    }
}
