namespace EventService.Domain.Events;

public sealed record EventQuestData(
    string Code,
    string DisplayName,
    string? Description,
    EventQuestKind Kind,
    EventQuestMetric Metric,
    int Target,
    int Points,
    int SortOrder);

public sealed record EventRewardData(
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
