namespace EventService.Contracts.Requests;

public sealed record EventQuestRequest(
    string Code,
    string DisplayName,
    string? Description,
    string Kind,
    string Metric,
    int Target,
    int Points,
    int SortOrder);
