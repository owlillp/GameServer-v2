namespace StoreService.Contracts.Dtos;

public sealed record OrderItemDto(
    Guid CatalogItemId,
    string Code,
    string Kind,
    string? Slot,
    int Quantity);
