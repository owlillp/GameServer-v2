namespace Shared.IntegrationEvents.Realtime.Notifications;

public sealed record EventLifecyclePayload(
    Guid EventId,
    string Code,
    string DisplayName,
    string CurrencyCode,
    DateTime StartsAt,
    DateTime EndsAt,
    DateTime GraceEndsAt);
