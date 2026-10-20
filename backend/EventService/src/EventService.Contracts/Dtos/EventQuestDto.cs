namespace EventService.Contracts.Dtos;

public sealed record EventQuestDto(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description,
    string Kind,
    string Metric,
    int Target,
    int Points,
    int SortOrder);
