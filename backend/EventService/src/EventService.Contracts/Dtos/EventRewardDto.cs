namespace EventService.Contracts.Dtos;

public sealed record EventRewardDto(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description,
    int PointsCost,
    Guid CatalogItemId,
    string ItemCode,
    string ItemKind,
    string? Slot,
    int Quantity,
    int SortOrder,
    bool IsRepeatable);
