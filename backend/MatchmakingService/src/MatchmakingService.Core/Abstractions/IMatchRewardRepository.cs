using MatchmakingService.Domain.Rewards;

namespace MatchmakingService.Core.Abstractions;

public interface IMatchRewardRepository
{
    Task<MatchRewardGrant?> GetAsync(Guid grantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MatchRewardGrant>> ListByMatchAsync(Guid matchId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MatchRewardGrant>> ListPendingAsync(int limit, CancellationToken cancellationToken);

    Task AddRangeAsync(IReadOnlyCollection<MatchRewardGrant> grants, CancellationToken cancellationToken);
}
