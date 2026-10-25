using ClanWarsService.Domain.Battles;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;

namespace ClanWarsService.Core.Abstractions;

public sealed record LeaderboardRow(Guid ClanId, long Points, double Score);

/// <summary>
/// Redis ZSET per season (<c>clanwars:season:{id}</c>); PostgreSQL stays the source of truth.
/// </summary>
public interface ISeasonLeaderboardStore
{
    Task SetAsync(Guid seasonId, IReadOnlyCollection<SeasonScore> scores, CancellationToken cancellationToken);

    Task ReplaceAsync(
        Guid seasonId,
        Func<Guid?, CancellationToken, Task<IReadOnlyList<SeasonScore>>> loadBatch,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaderboardRow>> GetRangeAsync(Guid seasonId, int offset, int limit, CancellationToken cancellationToken);

    Task<long?> GetRankAsync(Guid seasonId, Guid clanId, CancellationToken cancellationToken);

    Task<long> CountAsync(Guid seasonId, CancellationToken cancellationToken);
}

public interface IClanWarsEventPublisher
{
    Task PublishSeasonStartedAsync(Season season);

    Task PublishSeasonEndedAsync(Season season, IReadOnlyList<SeasonResult> top);

    Task PublishBattleCreatedAsync(Battle battle, string mode, string region);

    Task PublishBattleFinishedAsync(Battle battle, string status, BattleOutcome? outcome, DateTime finishedAt);
}

public interface IOutboxService
{
    Task PublishAsync<T>(T message)
        where T : class;
}

public interface ISeasonScheduler
{
    /// <summary>
    /// Makes Quartz triggers match the database: every open season gets its boundary triggers.
    /// </summary>
    Task ReconcileAsync(CancellationToken cancellationToken);

    Task ScheduleAsync(Season season, CancellationToken cancellationToken);
}
