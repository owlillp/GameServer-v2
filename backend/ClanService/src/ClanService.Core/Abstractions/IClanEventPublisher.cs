using ClanService.Domain.Clans;
using ClanService.Domain.Members;

namespace ClanService.Core.Abstractions;

public interface IClanEventPublisher
{
    Task PublishClanCreatedAsync(Clan clan);

    Task PublishClanDisbandedAsync(Clan clan, IReadOnlyList<Guid> memberAccountIds);

    Task PublishMemberJoinedAsync(ClanMember member);

    Task PublishMemberLeftAsync(ClanMember member, MemberLeaveReason reason, DateTime leftAt);

    Task PublishRoleChangedAsync(ClanMember member, ClanRole oldRole, Guid changedBy, DateTime changedAt);
}

public interface IOutboxService
{
    Task PublishAsync<T>(T message)
        where T : class;
}
