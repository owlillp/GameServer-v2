using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace EventService.Domain.Events;

public sealed class GameEvent
{
    private readonly List<EventQuest> _quests = [];
    private readonly List<EventReward> _rewards = [];

    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string? Description { get; private set; }

    public EventKind Kind { get; private set; }

    public EventStatus Status { get; private set; }

    public string CurrencyCode { get; private set; } = null!;

    public DateTime AnnounceAt { get; private set; }

    public DateTime StartsAt { get; private set; }

    public DateTime EndsAt { get; private set; }

    public DateTime GraceEndsAt { get; private set; }

    public DateTime? StartedNotifiedAt { get; private set; }

    public DateTime? EndedNotifiedAt { get; private set; }

    public uint Version { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<EventQuest> Quests => _quests;

    public IReadOnlyList<EventReward> Rewards => _rewards;

    // EF Core
    private GameEvent() { }

    private GameEvent(
        string code,
        string displayName,
        string? description,
        EventKind kind,
        string currencyCode,
        DateTime announceAt,
        DateTime startsAt,
        DateTime endsAt,
        DateTime graceEndsAt)
    {
        Id = Guid.CreateVersion7();
        Code = code;
        DisplayName = displayName;
        Description = description;
        Kind = kind;
        Status = EventStatus.DRAFT;
        CurrencyCode = currencyCode;
        AnnounceAt = announceAt;
        StartsAt = startsAt;
        EndsAt = endsAt;
        GraceEndsAt = graceEndsAt;
        Version = 1;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<GameEvent, Error> Create(
        string code,
        string displayName,
        string? description,
        EventKind kind,
        string currencyCode,
        DateTime? announceAt,
        DateTime startsAt,
        DateTime endsAt,
        DateTime? graceEndsAt,
        IReadOnlyList<EventQuestData> quests,
        IReadOnlyList<EventRewardData> rewards)
    {
        var codeResult = NormalizeCode(code);
        if (codeResult.IsFailure)
        {
            return codeResult.Error;
        }

        var nameResult = NormalizeDisplayName(displayName);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        var descriptionResult = NormalizeDescription(description);
        if (descriptionResult.IsFailure)
        {
            return descriptionResult.Error;
        }

        var currencyResult = NormalizeCurrency(currencyCode);
        if (currencyResult.IsFailure)
        {
            return currencyResult.Error;
        }

        DateTime normalizedStartsAt = NormalizeUtc(startsAt);
        DateTime normalizedEndsAt = NormalizeUtc(endsAt);
        DateTime normalizedAnnounceAt = announceAt is null ? normalizedStartsAt : NormalizeUtc(announceAt.Value);
        DateTime normalizedGraceEndsAt = graceEndsAt is null ? normalizedEndsAt : NormalizeUtc(graceEndsAt.Value);

        var scheduleResult = ValidateSchedule(normalizedAnnounceAt, normalizedStartsAt, normalizedEndsAt, normalizedGraceEndsAt);
        if (scheduleResult.IsFailure)
        {
            return scheduleResult.Error;
        }

        var gameEvent = new GameEvent(
            codeResult.Value,
            nameResult.Value,
            descriptionResult.Value,
            kind,
            currencyResult.Value,
            normalizedAnnounceAt,
            normalizedStartsAt,
            normalizedEndsAt,
            normalizedGraceEndsAt);

        var questsResult = gameEvent.ReplaceQuests(quests);
        if (questsResult.IsFailure)
        {
            return questsResult.Error;
        }

        var rewardsResult = gameEvent.ReplaceRewards(rewards);
        if (rewardsResult.IsFailure)
        {
            return rewardsResult.Error;
        }

        return gameEvent;
    }

    public UnitResult<Error> Update(
        string displayName,
        string? description,
        EventKind kind,
        string currencyCode,
        DateTime? announceAt,
        DateTime startsAt,
        DateTime endsAt,
        DateTime? graceEndsAt,
        IReadOnlyList<EventQuestData> quests,
        IReadOnlyList<EventRewardData> rewards)
    {
        if (Status == EventStatus.ARCHIVED)
        {
            return EventErrors.EventArchived;
        }

        if (Status != EventStatus.DRAFT)
        {
            return EventErrors.EventNotDraft;
        }

        var nameResult = NormalizeDisplayName(displayName);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        var descriptionResult = NormalizeDescription(description);
        if (descriptionResult.IsFailure)
        {
            return descriptionResult.Error;
        }

        var currencyResult = NormalizeCurrency(currencyCode);
        if (currencyResult.IsFailure)
        {
            return currencyResult.Error;
        }

        DateTime normalizedStartsAt = NormalizeUtc(startsAt);
        DateTime normalizedEndsAt = NormalizeUtc(endsAt);
        DateTime normalizedAnnounceAt = announceAt is null ? normalizedStartsAt : NormalizeUtc(announceAt.Value);
        DateTime normalizedGraceEndsAt = graceEndsAt is null ? normalizedEndsAt : NormalizeUtc(graceEndsAt.Value);

        var scheduleResult = ValidateSchedule(normalizedAnnounceAt, normalizedStartsAt, normalizedEndsAt, normalizedGraceEndsAt);
        if (scheduleResult.IsFailure)
        {
            return scheduleResult.Error;
        }

        var questsResult = ReplaceQuests(quests);
        if (questsResult.IsFailure)
        {
            return questsResult.Error;
        }

        var rewardsResult = ReplaceRewards(rewards);
        if (rewardsResult.IsFailure)
        {
            return rewardsResult.Error;
        }

        DisplayName = nameResult.Value;
        Description = descriptionResult.Value;
        Kind = kind;
        CurrencyCode = currencyResult.Value;
        AnnounceAt = normalizedAnnounceAt;
        StartsAt = normalizedStartsAt;
        EndsAt = normalizedEndsAt;
        GraceEndsAt = normalizedGraceEndsAt;
        Touch();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Publish()
    {
        if (Status == EventStatus.ARCHIVED)
        {
            return EventErrors.EventArchived;
        }

        if (Status == EventStatus.PUBLISHED)
        {
            return UnitResult.Success<Error>();
        }

        Status = EventStatus.PUBLISHED;
        Touch();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Archive()
    {
        if (Status == EventStatus.ARCHIVED)
        {
            return UnitResult.Success<Error>();
        }

        Status = EventStatus.ARCHIVED;
        Touch();

        return UnitResult.Success<Error>();
    }

    public EventPhase GetPhase(DateTime now)
    {
        if (Status == EventStatus.DRAFT)
        {
            return EventPhase.DRAFT;
        }

        if (Status == EventStatus.ARCHIVED)
        {
            return EventPhase.CLOSED;
        }

        DateTime utcNow = NormalizeUtc(now);

        if (utcNow < AnnounceAt)
        {
            return EventPhase.SCHEDULED;
        }

        if (utcNow < StartsAt)
        {
            return EventPhase.ANNOUNCED;
        }

        if (utcNow < EndsAt)
        {
            return EventPhase.ACTIVE;
        }

        if (utcNow < GraceEndsAt)
        {
            return EventPhase.GRACE;
        }

        return EventPhase.CLOSED;
    }

    public bool IsAcceptingProgress(DateTime now) =>
        Status == EventStatus.PUBLISHED && GetPhase(now) == EventPhase.ACTIVE;

    public bool IsAcceptingClaims(DateTime now) =>
        Status == EventStatus.PUBLISHED
        && GetPhase(now) is EventPhase.ACTIVE or EventPhase.GRACE;

    public EventReward? FindReward(Guid rewardId) =>
        _rewards.FirstOrDefault(reward => reward.Id == rewardId);

    public EventReward? FindRewardByCode(string code) =>
        _rewards.FirstOrDefault(reward =>
            string.Equals(reward.Code, code, StringComparison.OrdinalIgnoreCase));

    public void MarkStartedNotified(DateTime now)
    {
        StartedNotifiedAt = NormalizeUtc(now);
        Touch();
    }

    public void MarkEndedNotified(DateTime now)
    {
        EndedNotifiedAt = NormalizeUtc(now);
        Touch();
    }

    private UnitResult<Error> ReplaceQuests(IReadOnlyList<EventQuestData> quests)
    {
        if (quests.Count > EventConstants.MAX_QUESTS)
        {
            return GeneralErrors.ValueIsInvalid("quests");
        }

        if (quests.Select(quest => quest.Code.Trim().ToLowerInvariant()).Distinct(StringComparer.Ordinal).Count() != quests.Count)
        {
            return EventErrors.QuestCodeDuplicate;
        }

        _quests.Clear();
        foreach (EventQuestData data in quests)
        {
            var questResult = EventQuest.Create(Id, data);
            if (questResult.IsFailure)
            {
                return questResult.Error;
            }

            _quests.Add(questResult.Value);
        }

        return UnitResult.Success<Error>();
    }

    private UnitResult<Error> ReplaceRewards(IReadOnlyList<EventRewardData> rewards)
    {
        if (rewards.Count > EventConstants.MAX_REWARDS)
        {
            return GeneralErrors.ValueIsInvalid("rewards");
        }

        if (rewards.Select(reward => reward.Code.Trim().ToLowerInvariant()).Distinct(StringComparer.Ordinal).Count() != rewards.Count)
        {
            return EventErrors.RewardCodeDuplicate;
        }

        _rewards.Clear();
        foreach (EventRewardData data in rewards)
        {
            var rewardResult = EventReward.Create(Id, data);
            if (rewardResult.IsFailure)
            {
                return rewardResult.Error;
            }

            _rewards.Add(rewardResult.Value);
        }

        return UnitResult.Success<Error>();
    }

    private static UnitResult<Error> ValidateSchedule(
        DateTime announceAt,
        DateTime startsAt,
        DateTime endsAt,
        DateTime graceEndsAt)
    {
        if (startsAt >= endsAt || announceAt > startsAt || graceEndsAt < endsAt)
        {
            return EventErrors.ScheduleInvalid;
        }

        return UnitResult.Success<Error>();
    }

    private void Touch()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

    private static Result<string, Error> NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return GeneralErrors.ValueIsRequired("code");
        }

        string normalized = code.Trim().ToLowerInvariant();
        if (normalized.Length > EventConstants.CODE_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("code", max: EventConstants.CODE_MAX_LENGTH);
        }

        return normalized;
    }

    private static Result<string, Error> NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return GeneralErrors.ValueIsRequired("displayName");
        }

        string normalized = displayName.Trim();
        if (normalized.Length > EventConstants.DISPLAY_NAME_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("displayName", max: EventConstants.DISPLAY_NAME_MAX_LENGTH);
        }

        return normalized;
    }

    private static Result<string?, Error> NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return (string?)null;
        }

        string normalized = description.Trim();
        if (normalized.Length > EventConstants.DESCRIPTION_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("description", max: EventConstants.DESCRIPTION_MAX_LENGTH);
        }

        return normalized;
    }

    private static Result<string, Error> NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return GeneralErrors.ValueIsRequired("currencyCode");
        }

        string normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length > EventConstants.CURRENCY_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("currencyCode", max: EventConstants.CURRENCY_MAX_LENGTH);
        }

        return normalized;
    }
}
