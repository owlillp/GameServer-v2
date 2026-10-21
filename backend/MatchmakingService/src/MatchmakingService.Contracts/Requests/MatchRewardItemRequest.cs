namespace MatchmakingService.Contracts.Requests;

public sealed record MatchRewardItemRequest(
    Guid ItemId,
    string Code,
    string Kind,
    string? Slot,
    int Quantity);
