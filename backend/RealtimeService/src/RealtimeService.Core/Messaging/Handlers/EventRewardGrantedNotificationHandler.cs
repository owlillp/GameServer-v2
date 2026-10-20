using System.Text.Json;
using Shared.IntegrationEvents.Events.Events;
using Shared.IntegrationEvents.Realtime;
using Shared.IntegrationEvents.Realtime.Notifications;

namespace RealtimeService.Core.Messaging.Handlers;

public sealed class EventRewardGrantedNotificationHandler(IRealtimePublisher publisher)
{
    public Task Handle(EventRewardGrantedEvent @event, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new RealtimeNotification(
                NotificationId.Create($"event.reward.granted:{@event.ClaimId}"),
                RealtimeNotificationTypes.EVENT_REWARD_GRANTED,
                RealtimeAudiences.Player(@event.AccountId),
                @event.GrantedAt,
                Version: 1,
                RealtimeJson.ToElement(new EventRewardGrantedPayload(
                    @event.AccountId,
                    @event.EventId,
                    @event.RewardId,
                    @event.ClaimId,
                    @event.ItemIds))),
            cancellationToken);
}
