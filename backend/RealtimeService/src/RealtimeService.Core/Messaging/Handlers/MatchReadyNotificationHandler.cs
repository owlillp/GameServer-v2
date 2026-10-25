using Shared.IntegrationEvents.Matchmaking.Events;
using Shared.IntegrationEvents.Realtime;
using Shared.IntegrationEvents.Realtime.Notifications;

namespace RealtimeService.Core.Messaging.Handlers;

/// <summary>
/// Tells every participant that the match has a game server: the client then asks
/// GET /matchmaking/matches/me for the join ticket.
/// </summary>
public sealed class MatchReadyNotificationHandler(IRealtimePublisher publisher)
{
    public async Task Handle(MatchReadyEvent @event, CancellationToken cancellationToken)
    {
        foreach (Guid accountId in @event.AccountIds)
        {
            await publisher.PublishAsync(
                new RealtimeNotification(
                    NotificationId.Create($"matchmaking.match.ready:{@event.MatchId}:{accountId}"),
                    RealtimeNotificationTypes.MATCH_READY,
                    RealtimeAudiences.Player(accountId),
                    @event.ReadyAt,
                    Version: 1,
                    RealtimeJson.ToElement(new MatchReadyPayload(@event.MatchId, @event.Mode, @event.ServerAddress))),
                cancellationToken);
        }
    }
}
