namespace CatalogService.Contracts.Dtos;

public sealed record ItemDetailDto(
    Guid Id,
    string Code,
    string Kind,
    string Rarity,
    string Status,
    string DisplayName,
    string? Description,
    string? ModelCode,
    string? Slot,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> FitTags,
    long? LastPublishedVersion,
    IReadOnlyList<ItemStatDto> Stats,
    IReadOnlyList<ItemPriceDto> Prices,
    IReadOnlyList<AvailabilityWindowDto> Availability,
    DateTime CreatedAt,
    DateTime UpdatedAt);
