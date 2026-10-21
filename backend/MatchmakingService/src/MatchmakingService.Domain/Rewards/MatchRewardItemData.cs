namespace MatchmakingService.Domain.Rewards;

public sealed record MatchRewardItemData(
    Guid ItemId,
    string Code,
    string Kind,
    string? Slot,
    int Quantity);
