namespace Shared.IntegrationEvents.Realtime.Notifications;

public sealed record ItemsGrantedPayload(
    Guid AccountId,
    IReadOnlyList<Guid> ItemIds,
    string Source,
    Guid OperationId);
