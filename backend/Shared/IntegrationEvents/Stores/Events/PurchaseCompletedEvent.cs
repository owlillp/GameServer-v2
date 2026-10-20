namespace Shared.IntegrationEvents.Stores.Events;

public sealed record PurchaseCompletedEvent(
    Guid OrderId,
    Guid AccountId,
    Guid OfferId,
    string Currency,
    long TotalAmount,
    IReadOnlyList<Guid> ItemIds,
    DateTime CompletedAt);
