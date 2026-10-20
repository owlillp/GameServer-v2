using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace EventService.Domain.Progress;

public sealed class EventQuestProgress
{
    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public Guid QuestId { get; private set; }

    public Guid AccountId { get; private set; }

    public DateTime PeriodStart { get; private set; }

    public int Progress { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // EF Core
    private EventQuestProgress() { }

    private EventQuestProgress(Guid eventId, Guid questId, Guid accountId, DateTime periodStart)
    {
        Id = Guid.CreateVersion7();
        EventId = eventId;
        QuestId = questId;
        AccountId = accountId;
        PeriodStart = periodStart;
        Progress = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Result<EventQuestProgress, Error> Create(
        Guid eventId,
        Guid questId,
        Guid accountId,
        DateTime periodStart)
    {
        if (eventId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("eventId");
        }

        if (questId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("questId");
        }

        if (accountId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("accountId");
        }

        return new EventQuestProgress(eventId, questId, accountId, periodStart);
    }

    public bool Apply(int amount, int target)
    {
        if (amount <= 0 || target <= 0 || CompletedAt is not null)
        {
            return false;
        }

        Progress = Math.Min(Progress + amount, target);
        UpdatedAt = DateTime.UtcNow;

        if (Progress < target)
        {
            return false;
        }

        CompletedAt = UpdatedAt;
        return true;
    }
}
