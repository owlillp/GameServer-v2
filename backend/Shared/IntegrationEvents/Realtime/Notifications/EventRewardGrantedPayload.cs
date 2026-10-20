namespace Shared.IntegrationEvents.Realtime.Notifications;

public sealed record EventRewardGrantedPayload(
    Guid AccountId,
    Guid EventId,
    Guid RewardId,
    Guid ClaimId,
    IReadOnlyList<Guid> ItemIds);
