using ClanWarsService.Domain.Rewards;

namespace ClanWarsService.Core.Abstractions;

public sealed record RewardSummary(int Pending, int Granted, int Failed);

public interface IRewardRepository
{
    Task AddTableAsync(RewardTable table, CancellationToken cancellationToken);

    Task<RewardTable?> GetTableAsync(Guid tableId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RewardTable>> ListTablesAsync(CancellationToken cancellationToken);

    Task AddGrantsAsync(IReadOnlyCollection<SeasonRewardGrant> grants, CancellationToken cancellationToken);

    /// <summary>
    /// Pending grants due for an attempt, locked with <c>FOR UPDATE SKIP LOCKED</c>.
    /// </summary>
    Task<IReadOnlyList<SeasonRewardGrant>> ClaimDueAsync(DateTime now, int limit, CancellationToken cancellationToken);

    Task<SeasonRewardGrant?> GetGrantAsync(Guid grantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SeasonRewardGrant>> ListAsync(Guid seasonId, RewardGrantStatus? status, Guid? accountId, int limit, CancellationToken cancellationToken);

    Task<RewardSummary> SummarizeAsync(Guid seasonId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SeasonRewardGrant>> ListFailedAsync(Guid seasonId, CancellationToken cancellationToken);

    /// <summary>
    /// Delivered currency grants completed since <paramref name="since"/>, keyset by id.
    /// </summary>
    Task<IReadOnlyList<SeasonRewardGrant>> ListCreditedSinceAsync(DateTime since, Guid? afterId, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Grants whose items were sent before <paramref name="publishedBefore"/> and never confirmed.
    /// </summary>
    Task<IReadOnlyList<SeasonRewardGrant>> ListStuckItemsAsync(DateTime publishedBefore, int limit, CancellationToken cancellationToken);
}
