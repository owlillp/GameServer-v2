namespace CatalogService.Contracts.Dtos;

public sealed record CatalogVersionItemDto(
    Guid ItemId,
    string Code,
    string Kind,
    string Rarity,
    string DisplayName,
    string? ModelCode,
    string? Slot,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> FitTags);
