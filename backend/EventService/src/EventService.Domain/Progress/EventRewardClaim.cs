using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace EventService.Domain.Progress;

public sealed class EventRewardClaim
{
    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public Guid RewardId { get; private set; }

    public string RewardCode { get; private set; } = null!;

    public Guid AccountId { get; private set; }

    public int PointsSpent { get; private set; }

    public bool IsRepeatable { get; private set; }

    public EventRewardClaimStatus Status { get; private set; }

    public string? FailureReason { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    // EF Core
    private EventRewardClaim() { }

    private EventRewardClaim(
        Guid eventId,
        Guid rewardId,
        string rewardCode,
        Guid accountId,
        int pointsSpent,
        bool isRepeatable,
        string? idempotencyKey)
    {
        Id = Guid.CreateVersion7();
        EventId = eventId;
        RewardId = rewardId;
        RewardCode = rewardCode;
        AccountId = accountId;
        PointsSpent = pointsSpent;
        IsRepeatable = isRepeatable;
        Status = EventRewardClaimStatus.PENDING;
        IdempotencyKey = idempotencyKey;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<EventRewardClaim, Error> Create(
        Guid eventId,
        Guid rewardId,
        string rewardCode,
        Guid accountId,
        int pointsSpent,
        bool isRepeatable,
        string? idempotencyKey)
    {
        if (eventId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("eventId");
        }

        if (rewardId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("rewardId");
        }

        if (accountId == Guid.Empty)
        {
            return GeneralErrors.ValueIsRequired("accountId");
        }

        if (pointsSpent <= 0)
        {
            return EventErrors.RewardCostInvalid;
        }

        return new EventRewardClaim(
            eventId,
            rewardId,
            rewardCode.Trim().ToLowerInvariant(),
            accountId,
            pointsSpent,
            isRepeatable,
            string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim());
    }

    public UnitResult<Error> Complete(IReadOnlyList<Guid> itemIds)
    {
        if (Status != EventRewardClaimStatus.PENDING)
        {
            return UnitResult.Success<Error>();
        }

        if (itemIds.Count == 0)
        {
            return GeneralErrors.ValueIsRequired("items");
        }

        Status = EventRewardClaimStatus.COMPLETED;
        CompletedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Fail(string reason)
    {
        if (Status != EventRewardClaimStatus.PENDING)
        {
            return UnitResult.Success<Error>();
        }

        Status = EventRewardClaimStatus.FAILED;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Reward grant failed" : reason.Trim();
        CompletedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }
}
