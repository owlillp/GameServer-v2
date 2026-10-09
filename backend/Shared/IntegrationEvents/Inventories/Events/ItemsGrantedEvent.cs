namespace Shared.IntegrationEvents.Inventories.Events;

public record ItemsGrantedEvent(
    Guid AccountId,
    IReadOnlyList<Guid> ItemIds,
    string Source,
    Guid OperationId,
    DateTime OccurredAt);
