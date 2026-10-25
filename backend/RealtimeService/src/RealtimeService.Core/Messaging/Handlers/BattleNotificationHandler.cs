using Shared.IntegrationEvents.ClanWars.Events;
using Shared.IntegrationEvents.Realtime;

namespace RealtimeService.Core.Messaging.Handlers;

/// <summary>
/// Battle found / finished go to both clans' groups.
/// </summary>
public sealed class BattleNotificationHandler(IRealtimePublisher publisher)
{
    public async Task Handle(BattleFoundEvent @event, CancellationToken cancellationToken)
    {
        foreach (Guid clanId in new[] { @event.ClanAId, @event.ClanBId })
        {
            await PublishAsync($"clanwars.battle.found:{@event.BattleId}:{clanId}", RealtimeNotificationTypes.CLANWARS_BATTLE_FOUND, clanId, @event.FoundAt, @event, cancellationToken);
        }
    }

    public async Task Handle(BattleFinishedEvent @event, CancellationToken cancellationToken)
    {
        foreach (Guid clanId in new[] { @event.ClanAId, @event.ClanBId })
        {
            await PublishAsync($"clanwars.battle.finished:{@event.BattleId}:{clanId}", RealtimeNotificationTypes.CLANWARS_BATTLE_FINISHED, clanId, @event.FinishedAt, @event, cancellationToken);
        }
    }

    private Task PublishAsync<TPayload>(
        string id,
        string type,
        Guid clanId,
        DateTime occurredAt,
        TPayload payload,
        CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new RealtimeNotification(
                NotificationId.Create(id),
                type,
                RealtimeAudiences.Clan(clanId),
                occurredAt,
                Version: 1,
                RealtimeJson.ToElement(payload)),
            cancellationToken);
}
