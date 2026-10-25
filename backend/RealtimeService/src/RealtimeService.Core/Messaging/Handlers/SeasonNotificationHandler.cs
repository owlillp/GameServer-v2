using Shared.IntegrationEvents.ClanWars.Events;
using Shared.IntegrationEvents.Realtime;
using Shared.IntegrationEvents.Realtime.Notifications;

namespace RealtimeService.Core.Messaging.Handlers;

/// <summary>
/// Clan wars season start and end go to every connected player (live-ops audience).
/// </summary>
public sealed class SeasonNotificationHandler(IRealtimePublisher publisher)
{
    public Task Handle(SeasonStartedEvent @event, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new RealtimeNotification(
                NotificationId.Create($"clanwars.season.started:{@event.SeasonId}"),
                RealtimeNotificationTypes.CLANWARS_SEASON_STARTED,
                RealtimeAudiences.LIVE_OPS,
                @event.StartedAt,
                Version: 1,
                RealtimeJson.ToElement(new SeasonPayload(@event.SeasonId, @event.Number, @event.Name, @event.StartsAt, @event.EndsAt))),
            cancellationToken);

    public Task Handle(SeasonEndedEvent @event, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new RealtimeNotification(
                NotificationId.Create($"clanwars.season.ended:{@event.SeasonId}"),
                RealtimeNotificationTypes.CLANWARS_SEASON_ENDED,
                RealtimeAudiences.LIVE_OPS,
                @event.EndedAt,
                Version: 1,
                RealtimeJson.ToElement(@event)),
            cancellationToken);
}
