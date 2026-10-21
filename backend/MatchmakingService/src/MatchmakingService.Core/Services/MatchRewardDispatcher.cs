using MatchmakingService.Contracts.Communication;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Rewards;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;

namespace MatchmakingService.Core.Services;

public sealed class MatchRewardDispatcher(
    IMatchmakingEventPublisher eventPublisher,
    IWalletCreditClient walletClient,
    ITransactionManager transactionManager,
    ILogger<MatchRewardDispatcher> logger)
{
    public async Task DispatchAsync(MatchRewardGrant grant, CancellationToken cancellationToken)
    {
        if (grant.Status != MatchRewardStatus.Pending)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;

        if (grant.Items.Count > 0 && grant.ItemsPublishedAt is null)
        {
            await eventPublisher.PublishRewardGrantAsync(grant, cancellationToken);
            grant.MarkItemsPublished(now);
        }

        if (grant.Amount > 0 && grant.WalletCreditedAt is null)
        {
            var creditResult = await walletClient.CreditAsync(
                grant.AccountId,
                grant.CurrencyCode,
                grant.Amount,
                grant.WalletOperationId,
                $"match:{grant.MatchId}",
                cancellationToken);

            if (creditResult.IsSuccess)
            {
                grant.MarkWalletCredited(now);
            }
            else
            {
                string reason = creditResult.Error.ToString();
                grant.RecordFailure(reason, now);

                logger.LogWarning(
                    "Wallet credit failed for grant {GrantId} of account {AccountId}: {Error}",
                    grant.Id,
                    grant.AccountId,
                    reason);
            }
        }
        else if (grant.Amount == 0 && grant.WalletCreditedAt is null)
        {
            grant.MarkWalletCredited(now);
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            logger.LogError(
                "Failed to save reward grant {GrantId}: {Error}",
                grant.Id,
                saveResult.Error.ToString());
        }
    }
}
