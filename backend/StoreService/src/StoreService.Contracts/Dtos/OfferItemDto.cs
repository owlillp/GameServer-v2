namespace StoreService.Contracts.Dtos;

public sealed record OfferItemDto(
    Guid CatalogItemId,
    string Code,
    string Kind,
    string? Slot,
    int Quantity);
