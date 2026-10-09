namespace InventoryService.Contracts.Requests;

public sealed record GrantItemRequest(
    Guid ItemId,
    string Code,
    string Kind,
    string? Slot,
    int Quantity);
