namespace CatalogService.Contracts.Requests;

public sealed record CreateItemRequest(
    string Code,
    string Kind,
    string Rarity,
    string DisplayName,
    string? Description,
    string? ModelCode,
    string? Slot,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<string>? FitTags,
    IReadOnlyList<ItemStatRequest>? Stats,
    IReadOnlyList<ItemPriceRequest>? Prices,
    IReadOnlyList<AvailabilityWindowRequest>? Availability);
