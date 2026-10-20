namespace Shared.IntegrationEvents.Events.Commands;

public sealed record GrantEventRewardCommand(
    Guid ClaimId,
    Guid EventId,
    Guid RewardId,
    Guid AccountId,
    IReadOnlyList<GrantEventRewardItem> Items,
    DateTime RequestedAt);

public sealed record GrantEventRewardItem(
    Guid ItemId,
    string Code,
    string Kind,
    string? Slot,
    int Quantity);
