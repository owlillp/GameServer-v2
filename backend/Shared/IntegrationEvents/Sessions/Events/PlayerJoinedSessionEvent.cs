namespace Shared.IntegrationEvents.Sessions.Events;

public sealed record PlayerJoinedSessionEvent(
    Guid SessionId,
    Guid ServerId,
    Guid AccountId,
    string Kind,
    string Mode,
    string Region,
    DateTime JoinedAt);
