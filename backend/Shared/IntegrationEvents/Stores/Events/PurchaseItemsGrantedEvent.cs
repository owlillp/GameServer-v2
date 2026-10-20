namespace Shared.IntegrationEvents.Stores.Events;

public sealed record PurchaseItemsGrantedEvent(
    Guid OrderId,
    Guid AccountId,
    IReadOnlyList<Guid> ItemIds,
    DateTime OccurredAt);
