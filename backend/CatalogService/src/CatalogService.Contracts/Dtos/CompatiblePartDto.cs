namespace CatalogService.Contracts.Dtos;

public sealed record CompatiblePartDto(
    Guid ItemId,
    string Code,
    string DisplayName,
    string? Slot,
    string Rarity,
    bool IsUniversal);
