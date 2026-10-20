namespace EventService.Contracts.Dtos;

public sealed record EventProgressDto(
    Guid EventId,
    Guid AccountId,
    int PointsBalance,
    int TotalPointsEarned,
    IReadOnlyList<EventQuestProgressDto> Quests,
    DateTime? UpdatedAt);
