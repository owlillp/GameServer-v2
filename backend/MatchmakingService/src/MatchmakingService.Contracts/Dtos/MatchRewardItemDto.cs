namespace MatchmakingService.Contracts.Dtos;

public sealed record MatchRewardItemDto(
    Guid ItemId,
    string Code,
    string Kind,
    string? Slot,
    int Quantity);
