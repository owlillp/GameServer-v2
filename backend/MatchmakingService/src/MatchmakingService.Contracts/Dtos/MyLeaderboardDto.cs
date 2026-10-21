namespace MatchmakingService.Contracts.Dtos;

public sealed record MyLeaderboardDto(
    int? Rank,
    long Points,
    int Wins,
    int MatchesPlayed);
