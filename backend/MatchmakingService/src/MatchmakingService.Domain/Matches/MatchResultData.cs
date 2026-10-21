namespace MatchmakingService.Domain.Matches;

public sealed record MatchResultData(Guid AccountId, long Score, int Kills);
