namespace StoreService.Domain.Offers;

public sealed record OfferItemData(
    Guid CatalogItemId,
    string Code,
    string Kind,
    string? Slot,
    int Quantity);
