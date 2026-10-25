namespace Shared.IntegrationEvents.Clans.Events;

public sealed record ClanCreatedEvent(
    Guid ClanId,
    string Tag,
    string Name,
    Guid LeaderAccountId,
    DateTime CreatedAt);

/// <param name="MemberAccountIds">Members at the moment of disbanding, including the leader.</param>
public sealed record ClanDisbandedEvent(
    Guid ClanId,
    string Tag,
    IReadOnlyList<Guid> MemberAccountIds,
    DateTime DisbandedAt);

public sealed record ClanMemberJoinedEvent(
    Guid ClanId,
    Guid AccountId,
    string Role,
    DateTime JoinedAt);

/// <param name="Reason">left | kicked | disbanded.</param>
public sealed record ClanMemberLeftEvent(
    Guid ClanId,
    Guid AccountId,
    string Reason,
    DateTime LeftAt);

public sealed record ClanMemberRoleChangedEvent(
    Guid ClanId,
    Guid AccountId,
    string OldRole,
    string NewRole,
    Guid ChangedBy,
    DateTime ChangedAt);
