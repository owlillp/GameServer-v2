using EventService.Core.Abstractions;
using EventService.Domain.Progress;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.Events.Events;

namespace EventService.Core.Messaging.Handlers;

public sealed class EventRewardGrantedEventHandler(
    ILogger<EventRewardGrantedEventHandler> logger,
    IEventParticipationRepository participationRepository,
    ITransactionManager transactionManager)
{
    public async Task Handle(EventRewardGrantedEvent @event, CancellationToken cancellationToken)
    {
        var claimResult = await participationRepository.GetClaimAsync(@event.ClaimId, cancellationToken);
        if (claimResult.IsFailure)
        {
            logger.LogWarning("Reward claim {ClaimId} was not found on grant confirmation", @event.ClaimId);
            return;
        }

        EventRewardClaim claim = claimResult.Value;
        if (claim.Status != EventRewardClaimStatus.PENDING)
        {
            return;
        }

        var completeResult = claim.Complete(@event.ItemIds);
        if (completeResult.IsFailure)
        {
            logger.LogError(
                "Reward claim {ClaimId} could not be completed: {Error}",
                claim.Id,
                completeResult.Error.ToString());

            return;
        }

        await transactionManager.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Event reward {RewardId} granted to account {AccountId} for claim {ClaimId}",
            @event.RewardId,
            @event.AccountId,
            @event.ClaimId);
    }
}
