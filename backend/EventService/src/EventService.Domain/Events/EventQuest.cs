using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace EventService.Domain.Events;

public sealed class EventQuest
{
    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public string Code { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string? Description { get; private set; }

    public EventQuestKind Kind { get; private set; }

    public EventQuestMetric Metric { get; private set; }

    public int Target { get; private set; }

    public int Points { get; private set; }

    public int SortOrder { get; private set; }

    // EF Core
    private EventQuest() { }

    private EventQuest(
        Guid eventId,
        string code,
        string displayName,
        string? description,
        EventQuestKind kind,
        EventQuestMetric metric,
        int target,
        int points,
        int sortOrder)
    {
        Id = Guid.CreateVersion7();
        EventId = eventId;
        Code = code;
        DisplayName = displayName;
        Description = description;
        Kind = kind;
        Metric = metric;
        Target = target;
        Points = points;
        SortOrder = sortOrder;
    }

    internal static Result<EventQuest, Error> Create(Guid eventId, EventQuestData data)
    {
        if (eventId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("eventId");
        }

        if (string.IsNullOrWhiteSpace(data.Code))
        {
            return GeneralErrors.ValueIsRequired("quest.code");
        }

        string code = data.Code.Trim().ToLowerInvariant();
        if (code.Length > EventConstants.CODE_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("quest.code", max: EventConstants.CODE_MAX_LENGTH);
        }

        if (string.IsNullOrWhiteSpace(data.DisplayName))
        {
            return GeneralErrors.ValueIsRequired("quest.displayName");
        }

        string displayName = data.DisplayName.Trim();
        if (displayName.Length > EventConstants.DISPLAY_NAME_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("quest.displayName", max: EventConstants.DISPLAY_NAME_MAX_LENGTH);
        }

        string? description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim();
        if (description is not null && description.Length > EventConstants.DESCRIPTION_MAX_LENGTH)
        {
            return GeneralErrors.LengthIsInvalid("quest.description", max: EventConstants.DESCRIPTION_MAX_LENGTH);
        }

        if (data.Target <= 0)
        {
            return EventErrors.QuestTargetInvalid;
        }

        if (data.Points <= 0)
        {
            return EventErrors.QuestPointsInvalid;
        }

        return new EventQuest(
            eventId,
            code,
            displayName,
            description,
            data.Kind,
            data.Metric,
            data.Target,
            data.Points,
            data.SortOrder);
    }

    public DateTime GetPeriodStart(DateTime now, DateTime eventStartsAt) =>
        Kind switch
        {
            EventQuestKind.DAILY => StartOfDay(now),
            EventQuestKind.WEEKLY => StartOfWeek(now),
            EventQuestKind.SEASONAL => StartOfDay(eventStartsAt),
            _ => StartOfDay(now),
        };

    private static DateTime StartOfDay(DateTime value) =>
        DateTime.SpecifyKind(value.ToUniversalTime().Date, DateTimeKind.Utc);

    private static DateTime StartOfWeek(DateTime value)
    {
        DateTime day = StartOfDay(value);
        int offset = ((int)day.DayOfWeek + 6) % 7;
        return day.AddDays(-offset);
    }
}
