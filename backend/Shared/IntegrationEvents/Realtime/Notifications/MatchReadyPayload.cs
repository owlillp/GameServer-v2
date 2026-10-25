namespace Shared.IntegrationEvents.Realtime.Notifications;

public sealed record MatchReadyPayload(
    Guid MatchId,
    string Mode,
    string ServerAddress);
