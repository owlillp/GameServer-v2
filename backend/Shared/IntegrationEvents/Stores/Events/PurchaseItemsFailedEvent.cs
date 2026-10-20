namespace Shared.IntegrationEvents.Stores.Events;

public sealed record PurchaseItemsFailedEvent(
    Guid OrderId,
    Guid AccountId,
    string Reason,
    DateTime OccurredAt);
