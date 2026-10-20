namespace Shared.IntegrationEvents.Events.Events;

public sealed record EventStartedEvent(
    Guid EventId,
    string Code,
    string DisplayName,
    string CurrencyCode,
    DateTime StartsAt,
    DateTime EndsAt,
    DateTime GraceEndsAt,
    DateTime StartedAt);
