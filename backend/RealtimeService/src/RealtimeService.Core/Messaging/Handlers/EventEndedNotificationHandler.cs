using System.Text.Json;
using Shared.IntegrationEvents.Events.Events;
using Shared.IntegrationEvents.Realtime;
using Shared.IntegrationEvents.Realtime.Notifications;

namespace RealtimeService.Core.Messaging.Handlers;

public sealed class EventEndedNotificationHandler(IRealtimePublisher publisher)
{
    public Task Handle(EventEndedEvent @event, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new RealtimeNotification(
                NotificationId.Create($"event.ended:{@event.EventId}"),
                RealtimeNotificationTypes.EVENT_ENDED,
                RealtimeAudiences.LIVE_OPS,
                @event.EndedAt,
                Version: 1,
                RealtimeJson.ToElement(new EventEndedPayload(
                    @event.EventId,
                    @event.Code,
                    @event.EndedAt))),
            cancellationToken);
}
