using MatchmakingService.Domain.Leaderboard;

namespace MatchmakingService.Core.Abstractions;

public interface ILeaderboardRepository
{
    Task<LeaderboardEntry?> GetAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaderboardEntry>> ListByAccountsAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaderboardEntry>> ListAsync(int limit, CancellationToken cancellationToken);

    Task<int> CountWithMorePointsAsync(long points, CancellationToken cancellationToken);

    Task AddAsync(LeaderboardEntry entry, CancellationToken cancellationToken);

    Task AddRangeAsync(IReadOnlyCollection<LeaderboardEntry> entries, CancellationToken cancellationToken);
}

public sealed record LeaderboardRow(Guid AccountId, long Points);

public interface ILeaderboardStore
{
    Task RebuildAsync(IReadOnlyList<LeaderboardEntry> entries, CancellationToken cancellationToken);

    Task AddPointsAsync(Guid accountId, long points, CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaderboardRow>> GetTopAsync(int limit, CancellationToken cancellationToken);

    Task<long?> GetPointsAsync(Guid accountId, CancellationToken cancellationToken);

    Task<long?> GetRankAsync(Guid accountId, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}
