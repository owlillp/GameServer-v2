using EventService.Contracts.Dtos;
using EventService.Domain.Events;
using EventService.Domain.Progress;

namespace EventService.Core.Features.Events.Models;

public static class EventMapper
{
    public static EventDto ToDto(GameEvent gameEvent, DateTime now) =>
        new(
            gameEvent.Id,
            gameEvent.Code,
            gameEvent.DisplayName,
            gameEvent.Description,
            gameEvent.Kind.ToString(),
            gameEvent.Status.ToString(),
            gameEvent.GetPhase(now).ToString(),
            gameEvent.CurrencyCode,
            gameEvent.AnnounceAt,
            gameEvent.StartsAt,
            gameEvent.EndsAt,
            gameEvent.GraceEndsAt,
            [.. gameEvent.Quests.OrderBy(quest => quest.SortOrder).Select(ToQuestDto)],
            [.. gameEvent.Rewards.OrderBy(reward => reward.SortOrder).Select(ToRewardDto)],
            gameEvent.CreatedAt,
            gameEvent.UpdatedAt);

    public static EventProgressDto ToProgressDto(
        GameEvent gameEvent,
        EventProgress? progress,
        IReadOnlyList<EventQuestProgress> questProgresses,
        Guid accountId,
        DateTime now) =>
        new(
            gameEvent.Id,
            accountId,
            progress?.PointsBalance ?? 0,
            progress?.TotalPointsEarned ?? 0,
            [.. gameEvent.Quests
                .OrderBy(quest => quest.SortOrder)
                .Select(quest => ToQuestProgressDto(quest, questProgresses, gameEvent, now))],
            progress?.UpdatedAt);

    public static EventRewardClaimDto ToClaimDto(EventRewardClaim claim) =>
        new(
            claim.Id,
            claim.EventId,
            claim.RewardId,
            claim.RewardCode,
            claim.AccountId,
            claim.PointsSpent,
            claim.Status.ToString(),
            claim.FailureReason,
            claim.CreatedAt,
            claim.CompletedAt);

    private static EventQuestDto ToQuestDto(EventQuest quest) =>
        new(
            quest.Id,
            quest.Code,
            quest.DisplayName,
            quest.Description,
            quest.Kind.ToString(),
            quest.Metric.ToString(),
            quest.Target,
            quest.Points,
            quest.SortOrder);

    private static EventRewardDto ToRewardDto(EventReward reward) =>
        new(
            reward.Id,
            reward.Code,
            reward.DisplayName,
            reward.Description,
            reward.PointsCost,
            reward.CatalogItemId,
            reward.ItemCode,
            reward.ItemKind,
            reward.Slot,
            reward.Quantity,
            reward.SortOrder,
            reward.IsRepeatable);

    private static EventQuestProgressDto ToQuestProgressDto(
        EventQuest quest,
        IReadOnlyList<EventQuestProgress> questProgresses,
        GameEvent gameEvent,
        DateTime now)
    {
        DateTime periodStart = quest.GetPeriodStart(now, gameEvent.StartsAt);
        EventQuestProgress? progress = questProgresses.FirstOrDefault(entity =>
            entity.QuestId == quest.Id && entity.PeriodStart == periodStart);

        return new EventQuestProgressDto(
            quest.Id,
            quest.Code,
            progress?.Progress ?? 0,
            quest.Target,
            progress?.CompletedAt is not null,
            periodStart);
    }
}
