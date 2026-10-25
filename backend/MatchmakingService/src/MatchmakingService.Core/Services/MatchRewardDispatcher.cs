using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Domain.Rewards;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Database;
using WalletService.Contracts.Communication;

namespace MatchmakingService.Core.Services;

/// <summary>
/// Delivers reward grants. Grants are claimed with <c>FOR UPDATE SKIP LOCKED</c>, so two replicas
/// never process the same grant at once; the wallet credit is idempotent by the grant's operation id.
/// </summary>
public sealed class MatchRewardDispatcher(
    IMatchRewardRepository rewardRepository,
    IMatchmakingEventPublisher eventPublisher,
    IWalletCreditClient walletClient,
    ITransactionManager transactionManager,
    IOptionsMonitor<MatchmakingOptions> options,
    TimeProvider timeProvider,
    ILogger<MatchRewardDispatcher> logger)
{
    private const string WALLET_REASON = "MatchReward";

    /// <summary>
    /// Claims pending grants (all, or only <paramref name="grantIds"/>) and dispatches them in one transaction.
    /// </summary>
    public Task<int> DispatchPendingAsync(
        IReadOnlyCollection<Guid>? grantIds,
        CancellationToken cancellationToken) =>
        SkipLockedBatch.RunAsync(
            transactionManager,
            token => rewardRepository.ClaimPendingAsync(
                grantIds,
                grantIds?.Count ?? options.CurrentValue.RewardRetryBatchSize,
                token),
            DispatchClaimedAsync,
            cancellationToken);

    private async Task DispatchClaimedAsync(
        IReadOnlyList<MatchRewardGrant> grants,
        CancellationToken cancellationToken)
    {
        foreach (MatchRewardGrant grant in grants)
        {
            await DispatchAsync(grant, cancellationToken);
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        saveResult.ThrowIfFailed();
    }

    private async Task DispatchAsync(MatchRewardGrant grant, CancellationToken cancellationToken)
    {
        if (grant.Status != MatchRewardStatus.Pending)
        {
            return;
        }

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        if (grant.Items.Count > 0 && grant.ItemsPublishedAt is null)
        {
            await eventPublisher.PublishRewardGrantAsync(grant, cancellationToken);
            grant.MarkItemsPublished(now);
        }

        if (grant.WalletCreditedAt is not null)
        {
            return;
        }

        if (grant.Amount == 0)
        {
            grant.MarkWalletCredited(now);
            return;
        }

        var creditResult = await walletClient.CreditAsync(
            grant.AccountId,
            grant.CurrencyCode,
            grant.Amount,
            grant.WalletOperationId,
            WALLET_REASON,
            $"match:{grant.MatchId}",
            cancellationToken);

        if (creditResult.IsSuccess)
        {
            grant.MarkWalletCredited(now);
            return;
        }

        string reason = creditResult.Error.ToString();
        grant.RecordFailure(reason, now);

        logger.LogWarning(
            "Wallet credit failed for grant {GrantId} of account {AccountId}: {Error}",
            grant.Id,
            grant.AccountId,
            reason);
    }
}
