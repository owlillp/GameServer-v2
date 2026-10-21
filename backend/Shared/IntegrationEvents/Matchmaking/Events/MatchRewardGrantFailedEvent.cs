namespace Shared.IntegrationEvents.Matchmaking.Events;

public sealed record MatchRewardGrantFailedEvent(
    Guid GrantId,
    Guid MatchId,
    Guid AccountId,
    string Reason,
    DateTime FailedAt);
