namespace CatalogService.Contracts.Requests;

public sealed record UpdateItemRequest(
    string DisplayName,
    string? Description,
    string Rarity,
    string? ModelCode,
    string? Slot,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<string>? FitTags,
    IReadOnlyList<ItemStatRequest>? Stats,
    IReadOnlyList<ItemPriceRequest>? Prices,
    IReadOnlyList<AvailabilityWindowRequest>? Availability);
