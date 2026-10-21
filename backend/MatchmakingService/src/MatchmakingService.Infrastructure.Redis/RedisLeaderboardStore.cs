using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Domain.Leaderboard;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace MatchmakingService.Infrastructure.Redis;

public sealed class RedisLeaderboardStore(
    IConnectionMultiplexer connection,
    IOptions<MatchmakingOptions> options) : ILeaderboardStore
{
    private IDatabase Database => connection.GetDatabase();

    private RedisKey Key => options.Value.LeaderboardKey;

    public async Task RebuildAsync(
        IReadOnlyList<LeaderboardEntry> entries,
        CancellationToken cancellationToken)
    {
        await Database.KeyDeleteAsync(Key);

        if (entries.Count == 0)
        {
            return;
        }

        SortedSetEntry[] values = [.. entries.Select(entry => new SortedSetEntry(
            entry.AccountId.ToString(),
            entry.Points))];

        await Database.SortedSetAddAsync(Key, values);
    }

    public Task AddPointsAsync(Guid accountId, long points, CancellationToken cancellationToken) =>
        Database.SortedSetIncrementAsync(Key, accountId.ToString(), points);

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
                rows.Add(new LeaderboardRow(accountId, (long)entry.Score));
            }
        }

        return rows;
    }

    public async Task<long?> GetPointsAsync(Guid accountId, CancellationToken cancellationToken)
    {
        double? score = await Database.SortedSetScoreAsync(Key, accountId.ToString());

        return score is null ? null : (long)score.Value;
    }

    public async Task<long?> GetRankAsync(Guid accountId, CancellationToken cancellationToken)
    {
        long? rank = await Database.SortedSetRankAsync(Key, accountId.ToString(), Order.Descending);

        return rank;
    }

    public Task ClearAsync(CancellationToken cancellationToken) => Database.KeyDeleteAsync(Key);
}
