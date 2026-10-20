namespace Shared.IntegrationEvents.Stores.Commands;

public sealed record GrantPurchaseItemsCommand(
    Guid OrderId,
    Guid AccountId,
    IReadOnlyList<GrantPurchaseItem> Items,
    DateTime RequestedAt);

public sealed record GrantPurchaseItem(
    Guid ItemId,
    string Code,
    string Kind,
    string? Slot,
    int Quantity);
