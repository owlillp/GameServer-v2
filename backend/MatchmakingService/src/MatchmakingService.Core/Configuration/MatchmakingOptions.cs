using System.ComponentModel.DataAnnotations;

namespace MatchmakingService.Core.Configuration;

/// <summary>
/// Read through <c>IOptionsMonitor</c>: game design can change pairing parameters without a restart.
/// </summary>
public sealed class MatchmakingOptions
{
    public const string SECTION_NAME = "Matchmaking";

    [Range(1, 3600)]
    public int PairingIntervalSeconds { get; init; } = 10;

    [Range(1, 1000)]
    public int PairingBatchSize { get; init; } = 100;

    [Range(1, 1000)]
    public int RewardRetryBatchSize { get; init; } = 50;

    [Range(1, 24 * 30)]
    public int DefaultMatchDurationHours { get; init; } = 48;

    [Required]
    [MaxLength(32)]
    public string DefaultMode { get; init; } = "RANKED";

    [Required]
    [MaxLength(16)]
    public string DefaultCurrencyCode { get; init; } = "coins";

    [Range(0, long.MaxValue)]
    public long DefaultParticipationReward { get; init; } = 100;

    [Range(0, long.MaxValue)]
    public long DefaultWinBonus { get; init; } = 150;

    public bool RequireSamePartySize { get; init; } = true;

    public bool AutoFinishMatches { get; init; } = true;

    [Range(1, 1000)]
    public int ExpiredMatchBatchSize { get; init; } = 20;

    [Required]
    public string LeaderboardKey { get; init; } = "matchmaking:leaderboard:global";

    /// <summary>
    /// How often the Redis leaderboard is compared with PostgreSQL. A rebuild happens only on divergence.
    /// </summary>
    [Range(1, 24 * 60)]
    public int LeaderboardCheckMinutes { get; init; } = 5;

    /// <summary>
    /// Top entries compared member by member during the divergence check.
    /// </summary>
    [Range(1, 1000)]
    public int LeaderboardCheckSampleSize { get; init; } = 100;

    [Range(100, 10000)]
    public int LeaderboardRebuildBatchSize { get; init; } = 1000;
}
