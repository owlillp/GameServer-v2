using EventService.Domain.Events;
using Shared.IntegrationEvents.Events.Commands;
using Shared.IntegrationEvents.Events.Events;

namespace EventService.Core.Messaging.Publishers;

public interface IEventsEventPublisher
{
    Task PublishGrantRewardAsync(
        Guid claimId,
        Guid eventId,
        EventReward reward,
        Guid accountId,
        CancellationToken cancellationToken);

    Task PublishStartedAsync(GameEvent gameEvent, DateTime startedAt, CancellationToken cancellationToken);

    Task PublishEndedAsync(GameEvent gameEvent, DateTime endedAt, CancellationToken cancellationToken);
}

public sealed class EventsEventPublisher(IOutboxService outbox) : IEventsEventPublisher
{
    public Task PublishGrantRewardAsync(
        Guid claimId,
        Guid eventId,
        EventReward reward,
        Guid accountId,
        CancellationToken cancellationToken) =>
        outbox.PublishAsync(new GrantEventRewardCommand(
            claimId,
            eventId,
            reward.Id,
            accountId,
            [new GrantEventRewardItem(
                reward.CatalogItemId,
                reward.ItemCode,
                reward.ItemKind,
                reward.Slot,
                reward.Quantity)],
            DateTime.UtcNow));

    public Task PublishStartedAsync(
        GameEvent gameEvent,
        DateTime startedAt,
        CancellationToken cancellationToken) =>
        outbox.PublishAsync(new EventStartedEvent(
            gameEvent.Id,
            gameEvent.Code,
            gameEvent.DisplayName,
            gameEvent.CurrencyCode,
            gameEvent.StartsAt,
            gameEvent.EndsAt,
            gameEvent.GraceEndsAt,
            startedAt));

    public Task PublishEndedAsync(
        GameEvent gameEvent,
        DateTime endedAt,
        CancellationToken cancellationToken) =>
        outbox.PublishAsync(new EventEndedEvent(
            gameEvent.Id,
            gameEvent.Code,
            endedAt));
}
