using ClanService.Core.Abstractions;
using ClanService.Domain.Clans;
using ClanService.Domain.Members;
using Shared.IntegrationEvents.Clans.Events;

namespace ClanService.Core.Messaging.Publishers;

public sealed class ClanEventPublisher(IOutboxService outbox) : IClanEventPublisher
{
    public Task PublishClanCreatedAsync(Clan clan) =>
        outbox.PublishAsync(new ClanCreatedEvent(clan.Id, clan.Tag, clan.Name, clan.LeaderAccountId, clan.CreatedAt));

    public Task PublishClanDisbandedAsync(Clan clan, IReadOnlyList<Guid> memberAccountIds) =>
        outbox.PublishAsync(new ClanDisbandedEvent(
            clan.Id,
            clan.Tag,
            memberAccountIds,
            clan.DisbandedAt ?? DateTime.UtcNow));

    public Task PublishMemberJoinedAsync(ClanMember member) =>
        outbox.PublishAsync(new ClanMemberJoinedEvent(member.ClanId, member.AccountId, ToWire(member.Role), member.JoinedAt));

    public Task PublishMemberLeftAsync(ClanMember member, MemberLeaveReason reason, DateTime leftAt) =>
        outbox.PublishAsync(new ClanMemberLeftEvent(member.ClanId, member.AccountId, ToWire(reason), leftAt));

    public Task PublishRoleChangedAsync(ClanMember member, ClanRole oldRole, Guid changedBy, DateTime changedAt) =>
        outbox.PublishAsync(new ClanMemberRoleChangedEvent(
            member.ClanId,
            member.AccountId,
            ToWire(oldRole),
            ToWire(member.Role),
            changedBy,
            changedAt));

    private static string ToWire(ClanRole role) => role.ToString().ToLowerInvariant();

    private static string ToWire(MemberLeaveReason reason) => reason.ToString().ToLowerInvariant();
}
