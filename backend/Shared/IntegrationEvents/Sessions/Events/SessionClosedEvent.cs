namespace Shared.IntegrationEvents.Sessions.Events;

/// <param name="Reason">Why the session closed, e.g. "expired" when the server stopped sending heartbeats.</param>
public sealed record SessionClosedEvent(
    Guid SessionId,
    Guid ServerId,
    string Kind,
    Guid? MatchId,
    string Reason,
    DateTime ClosedAt);
