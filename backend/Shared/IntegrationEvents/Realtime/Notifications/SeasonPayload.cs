namespace Shared.IntegrationEvents.Realtime.Notifications;

public sealed record SeasonPayload(
    Guid SeasonId,
    int Number,
    string Name,
    DateTime StartsAt,
    DateTime EndsAt);
