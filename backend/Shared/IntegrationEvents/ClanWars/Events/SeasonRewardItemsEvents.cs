namespace Shared.IntegrationEvents.ClanWars.Events;

public sealed record SeasonRewardItemsGrantedEvent(
    Guid GrantId,
    Guid AccountId,
    IReadOnlyList<Guid> ItemIds,
    DateTime GrantedAt);

public sealed record SeasonRewardItemsFailedEvent(
    Guid GrantId,
    Guid AccountId,
    string Reason,
    DateTime FailedAt);
