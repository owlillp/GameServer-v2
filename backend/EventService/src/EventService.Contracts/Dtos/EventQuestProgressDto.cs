namespace EventService.Contracts.Dtos;

public sealed record EventQuestProgressDto(
    Guid QuestId,
    string QuestCode,
    int Progress,
    int Target,
    bool IsCompleted,
    DateTime PeriodStart);
