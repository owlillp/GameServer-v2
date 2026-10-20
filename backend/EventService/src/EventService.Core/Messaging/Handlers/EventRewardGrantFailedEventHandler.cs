using EventService.Core.Abstractions;
using EventService.Domain.Progress;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.Events.Events;

namespace EventService.Core.Messaging.Handlers;

public sealed class EventRewardGrantFailedEventHandler(
    ILogger<EventRewardGrantFailedEventHandler> logger,
    IEventParticipationRepository participationRepository,
    ITransactionManager transactionManager)
{
    public async Task Handle(EventRewardGrantFailedEvent @event, CancellationToken cancellationToken)
    {
        var claimResult = await participationRepository.GetClaimAsync(@event.ClaimId, cancellationToken);
        if (claimResult.IsFailure)
        {
            logger.LogWarning("Reward claim {ClaimId} was not found on grant failure", @event.ClaimId);
            return;
        }

        EventRewardClaim claim = claimResult.Value;
        if (claim.Status != EventRewardClaimStatus.PENDING)
        {
            return;
        }

        var failResult = claim.Fail(@event.Reason);
        if (failResult.IsFailure)
        {
            return;
        }

        var progressResult = await participationRepository.GetProgressAsync(
            claim.EventId,
            claim.AccountId,
            cancellationToken);

        if (progressResult.IsFailure)
        {
            logger.LogError(
                "Progress for event {EventId} and account {AccountId} was not found to refund claim {ClaimId}",
                claim.EventId,
                claim.AccountId,
                claim.Id);
        }
        else
        {
            progressResult.Value.RefundPoints(claim.PointsSpent);
        }

        await transactionManager.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Event reward claim {ClaimId} failed and {Points} points were refunded: {Reason}",
            claim.Id,
            claim.PointsSpent,
            @event.Reason);
    }
}
