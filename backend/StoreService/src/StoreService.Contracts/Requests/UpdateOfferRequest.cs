namespace StoreService.Contracts.Requests;

public sealed record UpdateOfferRequest(
    string DisplayName,
    string? Description,
    string PriceCurrency,
    long PriceAmount,
    DateTime? StartsAt,
    DateTime? EndsAt,
    int SortOrder,
    IReadOnlyList<OfferItemRequest> Items);
