using System.Text.Json;
using Shared.IntegrationEvents.Inventories.Events;
using Shared.IntegrationEvents.Realtime;
using Shared.IntegrationEvents.Realtime.Notifications;

namespace RealtimeService.Core.Messaging.Handlers;

public sealed class ItemsGrantedNotificationHandler(IRealtimePublisher publisher)
{
    public Task Handle(ItemsGrantedEvent @event, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new RealtimeNotification(
                NotificationId.Create($"inventory.items.granted:{@event.OperationId}"),
                RealtimeNotificationTypes.INVENTORY_ITEMS_GRANTED,
                RealtimeAudiences.Player(@event.AccountId),
                @event.OccurredAt,
                Version: 1,
                RealtimeJson.ToElement(new ItemsGrantedPayload(
                    @event.AccountId,
                    @event.ItemIds,
                    @event.Source,
                    @event.OperationId))),
            cancellationToken);
}
