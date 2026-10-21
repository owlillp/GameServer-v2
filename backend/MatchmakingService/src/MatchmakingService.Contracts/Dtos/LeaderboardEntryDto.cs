namespace MatchmakingService.Contracts.Dtos;

public sealed record LeaderboardEntryDto(
    int Rank,
    Guid AccountId,
    long Points,
    int Wins,
    int MatchesPlayed);
