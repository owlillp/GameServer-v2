namespace EventService.Contracts.Requests;

public sealed record EventRewardRequest(
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
