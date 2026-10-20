namespace Shared.IntegrationEvents.Events.Events;

public sealed record EventRewardGrantFailedEvent(
    Guid ClaimId,
    Guid EventId,
    Guid RewardId,
    Guid AccountId,
    string Reason,
    DateTime FailedAt);
