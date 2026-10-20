using System.Text.Json;
using Shared.IntegrationEvents.Events.Events;
using Shared.IntegrationEvents.Realtime;
using Shared.IntegrationEvents.Realtime.Notifications;

namespace RealtimeService.Core.Messaging.Handlers;

public sealed class EventStartedNotificationHandler(IRealtimePublisher publisher)
{
    public Task Handle(EventStartedEvent @event, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new RealtimeNotification(
                NotificationId.Create($"event.started:{@event.EventId}"),
                RealtimeNotificationTypes.EVENT_STARTED,
                RealtimeAudiences.LIVE_OPS,
                @event.StartedAt,
                Version: 1,
                RealtimeJson.ToElement(new EventLifecyclePayload(
                    @event.EventId,
                    @event.Code,
                    @event.DisplayName,
                    @event.CurrencyCode,
                    @event.StartsAt,
                    @event.EndsAt,
                    @event.GraceEndsAt))),
            cancellationToken);
}
