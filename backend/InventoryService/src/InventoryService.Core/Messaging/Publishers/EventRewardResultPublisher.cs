using Shared.IntegrationEvents.Events.Events;

namespace InventoryService.Core.Messaging.Publishers;

public interface IEventRewardResultPublisher
{
    Task PublishGrantedAsync(
        Guid claimId,
        Guid eventId,
        Guid rewardId,
        Guid accountId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken);

    Task PublishFailedAsync(
        Guid claimId,
        Guid eventId,
        Guid rewardId,
        Guid accountId,
        string reason,
        CancellationToken cancellationToken);
}

public sealed class EventRewardResultPublisher(IOutboxService outbox) : IEventRewardResultPublisher
{
    public Task PublishGrantedAsync(
        Guid claimId,
        Guid eventId,
        Guid rewardId,
        Guid accountId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken) =>
        outbox.PublishAsync(new EventRewardGrantedEvent(
            claimId,
            eventId,
            rewardId,
            accountId,
            itemIds,
            DateTime.UtcNow));

    public Task PublishFailedAsync(
        Guid claimId,
        Guid eventId,
        Guid rewardId,
        Guid accountId,
        string reason,
        CancellationToken cancellationToken) =>
        outbox.PublishAsync(new EventRewardGrantFailedEvent(
            claimId,
            eventId,
            rewardId,
            accountId,
            reason,
            DateTime.UtcNow));
}
