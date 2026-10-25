using ClanWarsService.Core.Abstractions;
using ClanWarsService.Core.Configuration;
using ClanWarsService.Domain;
using ClanWarsService.Domain.Scores;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ClanWarsService.Infrastructure.Redis;

/// <summary>
/// ZSET per season: absolute ZADD from PostgreSQL values, rebuild into a temporary key + RENAME.
/// </summary>
public sealed class RedisSeasonLeaderboardStore(
    IConnectionMultiplexer connection,
    IOptionsMonitor<ClanWarsOptions> options) : ISeasonLeaderboardStore
{
    private IDatabase Database => connection.GetDatabase();

    public async Task SetAsync(Guid seasonId, IReadOnlyCollection<SeasonScore> scores, CancellationToken cancellationToken)
    {
        if (scores.Count > 0)
        {
            await Database.SortedSetAddAsync(Key(seasonId), ToEntries(scores));
        }
    }

    public async Task ReplaceAsync(
        Guid seasonId,
        Func<Guid?, CancellationToken, Task<IReadOnlyList<SeasonScore>>> loadBatch,
        CancellationToken cancellationToken)
    {
        RedisKey key = Key(seasonId);
        RedisKey temporaryKey = $"{key}:rebuild:{Guid.NewGuid():N}";
        Guid? lastClanId = null;
        int total = 0;

        try
        {
            while (true)
            {
                IReadOnlyList<SeasonScore> batch = await loadBatch(lastClanId, cancellationToken);
                if (batch.Count == 0)
                {
                    break;
                }

                await Database.SortedSetAddAsync(temporaryKey, ToEntries(batch));
                lastClanId = batch[^1].ClanId;
                total += batch.Count;
            }

            if (total == 0)
            {
                await Database.KeyDeleteAsync(key);
                return;
            }

            await Database.KeyRenameAsync(temporaryKey, key);
        }
        catch
        {
            await Database.KeyDeleteAsync(temporaryKey);
            throw;
        }
    }

    public async Task<IReadOnlyList<LeaderboardRow>> GetRangeAsync(Guid seasonId, int offset, int limit, CancellationToken cancellationToken)
    {
        SortedSetEntry[] entries = await Database.SortedSetRangeByRankWithScoresAsync(
            Key(seasonId),
            offset,
            offset + limit - 1,
            Order.Descending);

        return [.. entries
            .Where(entry => Guid.TryParse(entry.Element.ToString(), out _))
            .Select(entry => new LeaderboardRow(Guid.Parse(entry.Element.ToString()), LeaderboardScore.DecodePoints(entry.Score), entry.Score))];
    }

    public Task<long?> GetRankAsync(Guid seasonId, Guid clanId, CancellationToken cancellationToken) =>
        Database.SortedSetRankAsync(Key(seasonId), clanId.ToString(), Order.Descending);

    public Task<long> CountAsync(Guid seasonId, CancellationToken cancellationToken) =>
        Database.SortedSetLengthAsync(Key(seasonId));

    private RedisKey Key(Guid seasonId) => $"{options.CurrentValue.LeaderboardKeyPrefix}{seasonId:N}";

    private static SortedSetEntry[] ToEntries(IEnumerable<SeasonScore> scores) =>
        [.. scores.Select(score => new SortedSetEntry(score.ClanId.ToString(), LeaderboardScore.Encode(score.Points, score.UpdatedAt)))];
}
