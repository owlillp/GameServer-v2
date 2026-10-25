using MatchmakingService.Domain.Leaderboard;

namespace MatchmakingService.Core.Abstractions;

public sealed record LeaderboardRow(Guid AccountId, long Points, double Score);

public interface ILeaderboardStore
{
    /// <summary>
    /// Writes absolute values (ZADD) taken from PostgreSQL, so a repeated write never double counts.
    /// </summary>
    Task SetAsync(IReadOnlyCollection<LeaderboardEntry> entries, CancellationToken cancellationToken);

    /// <summary>
    /// Builds a temporary key batch by batch and atomically swaps it in with RENAME.
    /// </summary>
    /// <param name="loadBatch">Receives the last account id of the previous batch (null for the first one).</param>
    Task ReplaceAsync(
        Func<Guid?, CancellationToken, Task<IReadOnlyList<LeaderboardEntry>>> loadBatch,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaderboardRow>> GetTopAsync(int limit, CancellationToken cancellationToken);

    Task<long?> GetRankAsync(Guid accountId, CancellationToken cancellationToken);

    Task<long> CountAsync(CancellationToken cancellationToken);
}
