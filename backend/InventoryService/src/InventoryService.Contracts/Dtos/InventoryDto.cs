namespace InventoryService.Contracts.Dtos;

public sealed record InventoryDto(
    Guid AccountId,
    IReadOnlyList<OwnedItemDto> Items,
    IReadOnlyList<OutfitEntryDto> Outfit,
    IReadOnlyList<CarDto> Cars,
    DateTime CreatedAt,
    DateTime UpdatedAt);
