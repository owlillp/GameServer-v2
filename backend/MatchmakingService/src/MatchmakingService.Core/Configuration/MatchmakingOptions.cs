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

    /// <summary>
    /// Pair parties of similar Elo rating and update ratings after queue matches.
    /// </summary>
    public bool RatingEnabled { get; init; } = true;

    [Range(1, 200)]
    public int RatingKFactor { get; init; } = 32;

    /// <summary>
    /// Allowed rating gap at once; grows by <see cref="RatingTolerancePerMinute"/> per waited minute.
    /// </summary>
    [Range(0, 10000)]
    public int RatingBaseTolerance { get; init; } = 100;

    [Range(0, 10000)]
    public int RatingTolerancePerMinute { get; init; } = 50;

    public ResultsSource ResultsSource { get; init; } = ResultsSource.Admin;

    [Required]
    [MaxLength(32)]
    public string DefaultRegion { get; init; } = "eu-west";

    /// <summary>
    /// A match without a game server after this long is cancelled and its parties re-queued.
    /// </summary>
    [Range(5, 3600)]
    public int AllocationTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// After this many failed allocations the parties are released instead of re-queued.
    /// </summary>
    [Range(0, 100)]
    public int MaxAllocationAttempts { get; init; } = 3;

    /// <summary>
    /// How long an external match (clan battle) may run before it is closed as expired.
    /// </summary>
    [Range(1, 24 * 60)]
    public int ExternalMatchDurationMinutes { get; init; } = 60;

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
