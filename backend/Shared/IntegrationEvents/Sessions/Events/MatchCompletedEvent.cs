namespace Shared.IntegrationEvents.Sessions.Events;

public sealed record MatchCompletedEvent(
    Guid MatchId,
    Guid AccountId,
    string Mode,
    bool IsWin,
    int Kills,
    int Score,
    DateTime CompletedAt);
