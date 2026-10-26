using System.Threading.Channels;
using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Domain;
using ClanWarsService.Domain.Scores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClanWarsService.Core.Services;

/// <summary>
/// Keeps a season ZSET equal to PostgreSQL: rebuilt only when it diverges, into a temporary key
/// swapped in with RENAME, by the replica that holds the season's advisory lock.
/// </summary>
public sealed class LeaderboardSync(
    IScoreRepository scoreRepository,
    ISeasonLeaderboardStore store,
    IOptionsMonitor<ClanWarsOptions> options,
    ILogger<LeaderboardSync> logger)
{
    public async Task<bool> SyncAsync(Guid seasonId, bool force, CancellationToken cancellationToken)
    {
        if (!force)
        {
            if (!await HasDivergedAsync(seasonId, cancellationToken))
            {
                return false;
            }

            ClanWarsMetrics.LeaderboardDivergences.Add(1);
        }

        await using IAsyncDisposable? rebuildLock = await scoreRepository.TryAcquireRebuildLockAsync(seasonId, cancellationToken);
        if (rebuildLock is null)
        {
            return false;
        }

        int batchSize = options.CurrentValue.LeaderboardRebuildBatchSize;
        await store.ReplaceAsync(
            seasonId,
            (afterClanId, token) => scoreRepository.ListAfterAsync(seasonId, afterClanId, batchSize, token),
            cancellationToken);

        ClanWarsMetrics.LeaderboardRebuilds.Add(1);
        logger.LogInformation("Leaderboard of season {SeasonId} rebuilt from PostgreSQL", seasonId);
        return true;
    }

    private async Task<bool> HasDivergedAsync(Guid seasonId, CancellationToken cancellationToken)
    {
        if (await scoreRepository.CountAsync(seasonId, cancellationToken) != await store.CountAsync(seasonId, cancellationToken))
        {
            return true;
        }

        int sample = options.CurrentValue.LeaderboardCheckSampleSize;
        IReadOnlyList<SeasonScore> expected = await scoreRepository.ListTopAsync(seasonId, 0, sample, cancellationToken);
        IReadOnlyList<LeaderboardRow> actual = await store.GetRangeAsync(seasonId, 0, sample, cancellationToken);

        return expected.Count != actual.Count
            || expected.Where((score, index) =>
                    score.ClanId != actual[index].ClanId
                    || LeaderboardScore.Encode(score.Points, score.UpdatedAt).CompareTo(actual[index].Score) != 0)
                .Any();
    }
}

/// <summary>
/// Lets request paths ask for an early divergence check of a season, e.g. after a failed Redis write.
/// </summary>
public sealed class LeaderboardSyncSignal
{
    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(64)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
    });

    public void Request(Guid seasonId) => _channel.Writer.TryWrite(seasonId);

    public async Task<Guid?> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            return await _channel.Reader.ReadAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
