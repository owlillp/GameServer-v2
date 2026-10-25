using MatchmakingService.Domain.Rewards;

namespace MatchmakingService.Core.Abstractions;

public interface IMatchRewardRepository
{
    Task<MatchRewardGrant?> GetAsync(Guid grantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MatchRewardGrant>> ListByMatchAsync(Guid matchId, CancellationToken cancellationToken);

    /// <summary>
    /// Locks pending grants with <c>FOR UPDATE SKIP LOCKED</c>; must run inside a transaction.
    /// When <paramref name="grantIds"/> is given, only those grants are claimed.
    /// </summary>
    Task<IReadOnlyList<MatchRewardGrant>> ClaimPendingAsync(
        IReadOnlyCollection<Guid>? grantIds,
        int limit,
        CancellationToken cancellationToken);

    Task AddRangeAsync(IReadOnlyCollection<MatchRewardGrant> grants, CancellationToken cancellationToken);
}
