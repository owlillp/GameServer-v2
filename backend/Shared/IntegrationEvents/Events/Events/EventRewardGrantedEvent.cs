namespace Shared.IntegrationEvents.Events.Events;

public sealed record EventRewardGrantedEvent(
    Guid ClaimId,
    Guid EventId,
    Guid RewardId,
    Guid AccountId,
    IReadOnlyList<Guid> ItemIds,
    DateTime GrantedAt);
