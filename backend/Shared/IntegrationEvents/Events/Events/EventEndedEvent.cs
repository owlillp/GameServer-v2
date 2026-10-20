namespace Shared.IntegrationEvents.Events.Events;

public sealed record EventEndedEvent(
    Guid EventId,
    string Code,
    DateTime EndedAt);
