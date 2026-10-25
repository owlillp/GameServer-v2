using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain.Rewards;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.ClanWars.Events;

namespace ClanWarsService.Core.Messaging.Handlers;

/// <summary>
/// Inventory's answer to a season reward items command.
/// </summary>
public sealed class RewardItemsHandler(
    IRewardRepository rewardRepository,
    ITransactionManager transactionManager,
    TimeProvider timeProvider,
    ILogger<RewardItemsHandler> logger)
{
    public async Task Handle(SeasonRewardItemsGrantedEvent @event, CancellationToken cancellationToken)
    {
        SeasonRewardGrant? grant = await rewardRepository.GetGrantAsync(@event.GrantId, cancellationToken);
        if (grant is null)
        {
            return;
        }

        grant.MarkItemsGranted(timeProvider.GetUtcNow().UtcDateTime);
        (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();
    }

    public async Task Handle(SeasonRewardItemsFailedEvent @event, CancellationToken cancellationToken)
    {
        SeasonRewardGrant? grant = await rewardRepository.GetGrantAsync(@event.GrantId, cancellationToken);
        if (grant is null || grant.ItemsGrantedAt is not null)
        {
            return;
        }

        grant.Fail($"inventory: {@event.Reason}");
        (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();

        logger.LogError("ALERT: reward items of grant {GrantId} were rejected: {Reason}", @event.GrantId, @event.Reason);
    }
}
