namespace MatchmakingService.Core.Configuration;

public sealed class MatchmakingOptions
{
    public const string SECTION_NAME = "Matchmaking";

    public int PairingIntervalSeconds { get; init; } = 10;

    public int RewardRetryIntervalSeconds { get; init; } = 30;

    public int DefaultMatchDurationHours { get; init; } = 48;

    public string DefaultMode { get; init; } = "RANKED";

    public string DefaultCurrencyCode { get; init; } = "coins";

    public long DefaultParticipationReward { get; init; } = 100;

    public long DefaultWinBonus { get; init; } = 150;

    public bool RequireSamePartySize { get; init; } = true;

    public bool AutoFinishMatches { get; init; } = true;

    public string LeaderboardKey { get; init; } = "matchmaking:leaderboard:global";

    public int LeaderboardSize { get; init; } = 1000;

    public int LeaderboardRefreshMinutes { get; init; } = 5;
}
