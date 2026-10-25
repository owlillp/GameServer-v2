using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Domain.Leaderboard;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace MatchmakingService.Infrastructure.Redis;

public sealed class RedisLeaderboardStore(
    IConnectionMultiplexer connection,
    IOptionsMonitor<MatchmakingOptions> options) : ILeaderboardStore
{
    private IDatabase Database => connection.GetDatabase();

    private RedisKey Key => options.CurrentValue.LeaderboardKey;

    public async Task SetAsync(IReadOnlyCollection<LeaderboardEntry> entries, CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return;
        }

        await Database.SortedSetAddAsync(Key, ToSortedSetEntries(entries));
    }

    public async Task ReplaceAsync(
        Func<Guid?, CancellationToken, Task<IReadOnlyList<LeaderboardEntry>>> loadBatch,
        CancellationToken cancellationToken)
    {
        RedisKey temporaryKey = $"{Key}:rebuild:{Guid.NewGuid():N}";
        Guid? lastAccountId = null;
        int total = 0;

        try
        {
            while (true)
            {
                IReadOnlyList<LeaderboardEntry> batch = await loadBatch(lastAccountId, cancellationToken);
                if (batch.Count == 0)
                {
                    break;
                }

                await Database.SortedSetAddAsync(temporaryKey, ToSortedSetEntries(batch));
                lastAccountId = batch[^1].AccountId;
                total += batch.Count;
            }

            if (total == 0)
            {
                await Database.KeyDeleteAsync(Key);
                return;
            }

            // RENAME replaces the live key atomically: readers see either the old or the new set.
            await Database.KeyRenameAsync(temporaryKey, Key);
        }
        catch
        {
            await Database.KeyDeleteAsync(temporaryKey);
            throw;
        }
    }

    public async Task<IReadOnlyList<LeaderboardRow>> GetTopAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        SortedSetEntry[] entries = await Database.SortedSetRangeByRankWithScoresAsync(
            Key,
            0,
            limit - 1,
            Order.Descending);

        var rows = new List<LeaderboardRow>(entries.Length);

        foreach (SortedSetEntry entry in entries)
        {
            if (Guid.TryParse(entry.Element.ToString(), out Guid accountId))
            {
                rows.Add(new LeaderboardRow(accountId, LeaderboardScore.DecodePoints(entry.Score), entry.Score));
            }
        }

        return rows;
    }

    public Task<long?> GetRankAsync(Guid accountId, CancellationToken cancellationToken) =>
        Database.SortedSetRankAsync(Key, accountId.ToString(), Order.Descending);

    public Task<long> CountAsync(CancellationToken cancellationToken) =>
        Database.SortedSetLengthAsync(Key);

    private static SortedSetEntry[] ToSortedSetEntries(IEnumerable<LeaderboardEntry> entries) =>
        [.. entries.Select(entry => new SortedSetEntry(
            entry.AccountId.ToString(),
            LeaderboardScore.Encode(entry.Points, entry.UpdatedAt)))];
}
