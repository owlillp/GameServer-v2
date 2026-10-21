namespace MatchmakingService.Contracts.Dtos;

public sealed record MatchResultDto(
    Guid AccountId,
    Guid PartyId,
    long Score,
    int Kills,
    bool IsWin,
    DateTime UpdatedAt);
