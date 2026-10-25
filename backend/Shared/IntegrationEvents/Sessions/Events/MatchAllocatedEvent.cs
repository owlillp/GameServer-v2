namespace Shared.IntegrationEvents.Sessions.Events;

public sealed record MatchAllocatedEvent(
    Guid MatchId,
    Guid SessionId,
    Guid ServerId,
    string ServerAddress,
    DateTime AllocatedAt);
