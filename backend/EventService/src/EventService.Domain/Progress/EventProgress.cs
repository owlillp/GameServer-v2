using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace EventService.Domain.Progress;

public sealed class EventProgress
{
    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public Guid AccountId { get; private set; }

    public int PointsBalance { get; private set; }

    public int TotalPointsEarned { get; private set; }

    public uint Version { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // EF Core
    private EventProgress() { }

    private EventProgress(Guid eventId, Guid accountId)
    {
        Id = Guid.CreateVersion7();
        EventId = eventId;
        AccountId = accountId;
        PointsBalance = 0;
        TotalPointsEarned = 0;
        Version = 1;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<EventProgress, Error> Create(Guid eventId, Guid accountId)
    {
        if (eventId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("eventId");
        }

        if (accountId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("accountId");
        }

        return new EventProgress(eventId, accountId);
    }

    public UnitResult<Error> AddPoints(int points)
    {
        if (points <= 0)
        {
            return EventErrors.QuestPointsInvalid;
        }

        PointsBalance += points;
        TotalPointsEarned += points;
        Touch();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> RefundPoints(int points)
    {
        if (points <= 0)
        {
            return EventErrors.RewardCostInvalid;
        }

        PointsBalance += points;
        Touch();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> SpendPoints(int points)
    {
        if (points <= 0)
        {
            return EventErrors.RewardCostInvalid;
        }

        if (PointsBalance < points)
        {
            return EventErrors.InsufficientPoints;
        }

        PointsBalance -= points;
        Touch();

        return UnitResult.Success<Error>();
    }

    private void Touch()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }
}
