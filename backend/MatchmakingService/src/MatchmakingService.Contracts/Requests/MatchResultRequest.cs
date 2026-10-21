namespace MatchmakingService.Contracts.Requests;

public sealed record MatchResultRequest(
    Guid AccountId,
    long Score,
    int Kills);
