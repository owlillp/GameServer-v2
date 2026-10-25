using RealtimeService.Core.Abstractions;
using Shared.IntegrationEvents.Clans.Events;
using Shared.IntegrationEvents.Realtime;
using Shared.IntegrationEvents.Realtime.Notifications;

namespace RealtimeService.Core.Messaging.Handlers;

/// <summary>
/// Keeps the clan roster projection and the <c>clan:{id}</c> SignalR groups in sync with
/// ClanService and pushes roster changes to the whole clan.
/// </summary>
public sealed class ClanNotificationHandler(
    IClanRosterStore rosterStore,
    IRealtimeGroups groups,
    IRealtimePublisher publisher)
{
    public Task Handle(ClanCreatedEvent @event, CancellationToken cancellationToken) =>
        JoinAsync(@event.LeaderAccountId, @event.ClanId, cancellationToken);

    public async Task Handle(ClanMemberJoinedEvent @event, CancellationToken cancellationToken)
    {
        await JoinAsync(@event.AccountId, @event.ClanId, cancellationToken);

        await PublishAsync(
            $"clan.member.joined:{@event.ClanId}:{@event.AccountId}:{@event.JoinedAt.Ticks}",
            RealtimeNotificationTypes.CLAN_MEMBER_JOINED,
            @event.ClanId,
            @event.JoinedAt,
            new ClanMemberPayload(@event.ClanId, @event.AccountId, @event.Role, Reason: null),
            cancellationToken);
    }

    public async Task Handle(ClanMemberLeftEvent @event, CancellationToken cancellationToken)
    {
        // Pushed before leaving the group, so the player who left gets it too.
        await PublishAsync(
            $"clan.member.left:{@event.ClanId}:{@event.AccountId}:{@event.LeftAt.Ticks}",
            RealtimeNotificationTypes.CLAN_MEMBER_LEFT,
            @event.ClanId,
            @event.LeftAt,
            new ClanMemberPayload(@event.ClanId, @event.AccountId, Role: null, @event.Reason),
            cancellationToken);

        await LeaveAsync(@event.AccountId, @event.ClanId, cancellationToken);
    }

    public Task Handle(ClanMemberRoleChangedEvent @event, CancellationToken cancellationToken) =>
        PublishAsync(
            $"clan.member.role:{@event.ClanId}:{@event.AccountId}:{@event.ChangedAt.Ticks}",
            RealtimeNotificationTypes.CLAN_MEMBER_ROLE_CHANGED,
            @event.ClanId,
            @event.ChangedAt,
            new ClanMemberPayload(@event.ClanId, @event.AccountId, @event.NewRole, Reason: null),
            cancellationToken);

    public async Task Handle(ClanDisbandedEvent @event, CancellationToken cancellationToken)
    {
        await PublishAsync(
            $"clan.disbanded:{@event.ClanId}",
            RealtimeNotificationTypes.CLAN_DISBANDED,
            @event.ClanId,
            @event.DisbandedAt,
            new ClanDisbandedPayload(@event.ClanId, @event.Tag),
            cancellationToken);

        foreach (Guid accountId in @event.MemberAccountIds)
        {
            await LeaveAsync(accountId, @event.ClanId, cancellationToken);
        }
    }

    private async Task JoinAsync(Guid accountId, Guid clanId, CancellationToken cancellationToken)
    {
        await rosterStore.SetClanAsync(accountId, clanId, cancellationToken);

        foreach (string connectionId in await rosterStore.GetConnectionsAsync(accountId, cancellationToken))
        {
            await groups.AddAsync(connectionId, RealtimeAudiences.Clan(clanId), cancellationToken);
        }
    }

    private async Task LeaveAsync(Guid accountId, Guid clanId, CancellationToken cancellationToken)
    {
        await rosterStore.RemoveClanAsync(accountId, clanId, cancellationToken);

        foreach (string connectionId in await rosterStore.GetConnectionsAsync(accountId, cancellationToken))
        {
            await groups.RemoveAsync(connectionId, RealtimeAudiences.Clan(clanId), cancellationToken);
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
