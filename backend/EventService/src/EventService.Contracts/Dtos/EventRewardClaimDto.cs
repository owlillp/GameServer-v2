namespace EventService.Contracts.Dtos;

public sealed record EventRewardClaimDto(
    Guid Id,
    Guid EventId,
    Guid RewardId,
    string RewardCode,
    Guid AccountId,
    int PointsSpent,
    string Status,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? CompletedAt);
