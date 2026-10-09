namespace CatalogService.Contracts.Dtos;

public sealed record ItemDto(
    Guid Id,
    string Code,
    string Kind,
    string Rarity,
    string DisplayName,
    string? ModelCode,
    string? Slot,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> FitTags,
    long? MinPrice,
    string? MinPriceCurrency,
    DateTime CreatedAt,
    DateTime UpdatedAt);
