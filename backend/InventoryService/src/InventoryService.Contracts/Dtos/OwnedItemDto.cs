namespace InventoryService.Contracts.Dtos;

public sealed record OwnedItemDto(
    Guid ItemId,
    string Code,
    string Kind,
    string? Slot,
    string Source,
    int Quantity,
    DateTime AcquiredAt);
