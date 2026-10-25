namespace Shared.IntegrationEvents.Matchmaking.Events;

/// <summary>
/// A queued match got a game server; players can request a join ticket.
/// </summary>
public sealed record MatchReadyEvent(
    Guid MatchId,
    string Mode,
    string ServerAddress,
    IReadOnlyList<Guid> AccountIds,
    DateTime ReadyAt);
