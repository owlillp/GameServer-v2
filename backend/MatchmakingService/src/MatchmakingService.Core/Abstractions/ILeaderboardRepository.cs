using MatchmakingService.Domain.Leaderboard;

namespace MatchmakingService.Core.Abstractions;

public interface ILeaderboardRepository
{
    Task<LeaderboardEntry?> GetAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>
    /// Ordered by points, ties broken by who reached the points first (same order as Redis).
    /// </summary>
    Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(int offset, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaderboardEntry>> ListByAccountsAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Keyset page ordered by account id: stable while points change, used for rebuilds.
    /// </summary>
    Task<IReadOnlyList<LeaderboardEntry>> ListAfterAsync(
        Guid? afterAccountId,
        int limit,
        CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    Task<int> CountAheadOfAsync(LeaderboardEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Atomic <c>points = points + delta</c> upsert, executed in the current transaction.
    /// </summary>
    Task ApplyResultAsync(Guid accountId, long points, bool isWin, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// Session-level <c>pg_try_advisory_lock</c>; the returned handle releases it on dispose.
    /// Returns null when another replica holds the lock.
    /// </summary>
    Task<IAsyncDisposable?> TryAcquireRebuildLockAsync(CancellationToken cancellationToken);
}
