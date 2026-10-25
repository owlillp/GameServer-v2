using ClanWarsService.Core.Abstractions;
using ClanWarsService.Domain.Roster;
using Shared.Core.Database;
using Shared.IntegrationEvents.Clans.Events;

namespace ClanWarsService.Core.Messaging.Handlers;

/// <summary>
/// Builds the clan and roster projection from ClanService events. Wolverine's durable inbox drops
/// redelivered messages; the event time on each roster row drops events that arrive out of order.
/// </summary>
public sealed class ClanRosterHandler(
    IRosterRepository rosterRepository,
    ITransactionManager transactionManager)
{
    private const string LEADER_ROLE = "leader";

    public async Task Handle(ClanCreatedEvent @event, CancellationToken cancellationToken)
    {
        if (await rosterRepository.GetClanAsync(@event.ClanId, cancellationToken) is null)
        {
            await rosterRepository.AddClanAsync(
                new ClanProjection(@event.ClanId, @event.Tag, @event.Name, @event.CreatedAt),
                cancellationToken);
        }

        await UpsertAsync(@event.LeaderAccountId, @event.ClanId, LEADER_ROLE, @event.CreatedAt, cancellationToken);
        await SaveAsync(cancellationToken);
    }

    public async Task Handle(ClanMemberJoinedEvent @event, CancellationToken cancellationToken)
    {
        await UpsertAsync(@event.AccountId, @event.ClanId, @event.Role, @event.JoinedAt, cancellationToken);
        await SaveAsync(cancellationToken);
    }

    public async Task Handle(ClanMemberRoleChangedEvent @event, CancellationToken cancellationToken)
    {
        RosterEntry? entry = await rosterRepository.GetEntryAsync(@event.AccountId, cancellationToken);
        if (entry is null || entry.ClanId != @event.ClanId || entry.IsNewerThan(@event.ChangedAt))
        {
            return;
        }

        entry.Apply(@event.ClanId, @event.NewRole, @event.ChangedAt);
        await SaveAsync(cancellationToken);
    }

    public async Task Handle(ClanMemberLeftEvent @event, CancellationToken cancellationToken)
    {
        await RemoveAsync(@event.AccountId, @event.ClanId, @event.LeftAt, cancellationToken);
        await SaveAsync(cancellationToken);
    }

    public async Task Handle(ClanDisbandedEvent @event, CancellationToken cancellationToken)
    {
        ClanProjection? clan = await rosterRepository.GetClanAsync(@event.ClanId, cancellationToken);
        clan?.MarkDisbanded(@event.DisbandedAt);

        foreach (Guid accountId in @event.MemberAccountIds)
        {
            await RemoveAsync(accountId, @event.ClanId, @event.DisbandedAt, cancellationToken);
        }

        await SaveAsync(cancellationToken);
    }

    private async Task UpsertAsync(Guid accountId, Guid clanId, string role, DateTime at, CancellationToken cancellationToken)
    {
        RosterEntry? entry = await rosterRepository.GetEntryAsync(accountId, cancellationToken);

        if (entry is null)
        {
            await rosterRepository.AddEntryAsync(new RosterEntry(accountId, clanId, role, at), cancellationToken);
        }
        else if (!entry.IsNewerThan(at))
        {
            entry.Apply(clanId, role, at);
        }
    }

    private async Task RemoveAsync(Guid accountId, Guid clanId, DateTime at, CancellationToken cancellationToken)
    {
        RosterEntry? entry = await rosterRepository.GetEntryAsync(accountId, cancellationToken);
        if (entry is not null && entry.ClanId == clanId && !entry.IsNewerThan(at))
        {
            rosterRepository.RemoveEntry(entry);
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);

        // A concurrent delivery inserted the same clan or player first; the inbox will not replay it,
        // so retry to apply this event on top of it.
        saveResult.ThrowIfFailed();
    }
}
