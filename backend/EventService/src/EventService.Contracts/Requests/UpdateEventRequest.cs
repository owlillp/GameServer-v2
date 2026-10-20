namespace EventService.Contracts.Requests;

public sealed record UpdateEventRequest(
    string DisplayName,
    string? Description,
    string Kind,
    string CurrencyCode,
    DateTime? AnnounceAt,
    DateTime StartsAt,
    DateTime EndsAt,
    DateTime? GraceEndsAt,
    IReadOnlyList<EventQuestRequest> Quests,
    IReadOnlyList<EventRewardRequest> Rewards);
