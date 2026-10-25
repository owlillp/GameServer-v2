namespace Shared.IntegrationEvents.Sessions.Events;

public sealed record PlayerLeftSessionEvent(
    Guid SessionId,
    Guid ServerId,
    Guid AccountId,
    DateTime LeftAt);
