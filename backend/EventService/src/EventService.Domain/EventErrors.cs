using Shared.SharedKernel.Errors;

namespace EventService.Domain;

public static class EventErrors
{
    public static Error EventNotFound { get; } =
        Error.Validation("events.event.not_found", "Event was not found", "eventCode");

    public static Error EventCodeDuplicate { get; } =
        Error.Conflict("events.event.code.duplicate", "Event code already exists", "code");

    public static Error EventNotDraft { get; } =
        Error.Validation("events.event.not_draft", "Published or archived event cannot be changed", "eventId");

    public static Error EventArchived { get; } =
        Error.Validation("events.event.archived", "Archived event cannot be changed", "eventId");

    public static Error ScheduleInvalid { get; } =
        Error.Validation("events.event.schedule.invalid", "Event schedule is invalid", "schedule");

    public static Error KindInvalid { get; } =
        Error.Validation("events.event.kind.invalid", "Event kind is invalid", "kind");

    public static Error QuestKindInvalid { get; } =
        Error.Validation("events.quest.kind.invalid", "Quest kind is invalid", "quests");

    public static Error QuestMetricInvalid { get; } =
        Error.Validation("events.quest.metric.invalid", "Quest metric is invalid", "quests");

    public static Error QuestCodeDuplicate { get; } =
        Error.Validation("events.quest.code.duplicate", "Quest code is duplicated inside the event", "quests");

    public static Error RewardCodeDuplicate { get; } =
        Error.Validation("events.reward.code.duplicate", "Reward code is duplicated inside the event", "rewards");

    public static Error QuestTargetInvalid { get; } =
        Error.Validation("events.quest.target.invalid", "Quest target must be positive", "quests");

    public static Error QuestPointsInvalid { get; } =
        Error.Validation("events.quest.points.invalid", "Quest points must be positive", "quests");

    public static Error RewardCostInvalid { get; } =
        Error.Validation("events.reward.cost.invalid", "Reward cost must be positive", "rewards");

    public static Error QuantityInvalid { get; } =
        Error.Validation("events.quantity.invalid", "Quantity must be positive", "quantity");

    public static Error EventNotAcceptingProgress { get; } =
        Error.Validation("events.event.not_active", "Event is not accepting progress", "eventId");

    public static Error EventNotAcceptingClaims { get; } =
        Error.Validation("events.event.not_claimable", "Event is closed for reward claims", "eventId");

    public static Error RewardNotFound { get; } =
        Error.Validation("events.reward.not_found", "Event reward was not found", "rewardCode");

    public static Error InsufficientPoints { get; } =
        Error.Validation("events.progress.insufficient_points", "Not enough event points", "points");

    public static Error RewardAlreadyClaimed { get; } =
        Error.Conflict("events.reward.already_claimed", "Reward has already been claimed", "rewardId");

    public static Error ClaimNotFound { get; } =
        Error.Validation("events.claim.not_found", "Reward claim was not found", "claimId");

    public static Error ClaimNotPending { get; } =
        Error.Validation("events.claim.not_pending", "Reward claim is already finalized", "claimId");

    public static Error ProgressNotFound { get; } =
        Error.Validation("events.progress.not_found", "Event progress was not found", "accountId");
}
