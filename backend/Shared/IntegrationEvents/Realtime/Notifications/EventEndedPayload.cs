namespace Shared.IntegrationEvents.Realtime.Notifications;

public sealed record EventEndedPayload(
    Guid EventId,
    string Code,
    DateTime EndedAt);
