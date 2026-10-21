namespace Shared.IntegrationEvents.Matchmaking.Events;

public sealed record MatchRewardGrantedEvent(
    Guid GrantId,
    Guid MatchId,
    Guid AccountId,
    IReadOnlyList<Guid> ItemIds,
    DateTime GrantedAt);
