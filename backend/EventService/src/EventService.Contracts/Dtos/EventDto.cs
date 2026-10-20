namespace EventService.Contracts.Dtos;

public sealed record EventDto(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description,
    string Kind,
    string Status,
    string Phase,
    string CurrencyCode,
    DateTime AnnounceAt,
    DateTime StartsAt,
    DateTime EndsAt,
    DateTime GraceEndsAt,
    IReadOnlyList<EventQuestDto> Quests,
    IReadOnlyList<EventRewardDto> Rewards,
    DateTime CreatedAt,
    DateTime UpdatedAt);
