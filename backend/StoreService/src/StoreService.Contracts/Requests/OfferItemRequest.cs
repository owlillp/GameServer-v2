namespace StoreService.Contracts.Requests;

public sealed record OfferItemRequest(
    Guid CatalogItemId,
    int Quantity);
