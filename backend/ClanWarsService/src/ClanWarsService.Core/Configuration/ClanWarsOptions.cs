using System.ComponentModel.DataAnnotations;

namespace ClanWarsService.Core.Configuration;

public sealed class ClanWarsOptions
{
    public const string SECTION_NAME = "ClanWars";

    /// <summary>
    /// Default time after the season end during which battles that started before it may report.
    /// </summary>
    [Range(0, 24 * 60)]
    public int DefaultFinalizationGraceMinutes { get; init; } = 10;

    /// <summary>
    /// How many top clans are put into season-ended.clanwars.
    /// </summary>
    [Range(1, 1000)]
    public int EndedEventTopSize { get; init; } = 10;

    [Range(1, 1000)]
    public int MaxLeaderboardLimit { get; init; } = 100;

    [Range(0, 50)]
    public int MaxNeighbors { get; init; } = 10;

    [Required]
    public string LeaderboardKeyPrefix { get; init; } = "clanwars:season:";

    [Range(1, 24 * 60)]
    public int LeaderboardCheckMinutes { get; init; } = 5;

    [Range(1, 1000)]
    public int LeaderboardCheckSampleSize { get; init; } = 100;

    [Range(100, 10000)]
    public int LeaderboardRebuildBatchSize { get; init; } = 1000;

    public bool QuartzClustered { get; init; } = true;

    [Range(1, 3600)]
    public int QuartzMisfireThresholdSeconds { get; init; } = 60;

    [Range(1, 3600)]
    public int ReconcileRetrySeconds { get; init; } = 15;

    [Required]
    [MaxLength(32)]
    public string BattleRegion { get; init; } = "eu-west";

    [Range(1, 10)]
    public int MinSquadSize { get; init; } = 1;

    [Range(1, 10)]
    public int MaxSquadSize { get; init; } = 5;

    [Range(1, 3600)]
    public int MatcherIntervalSeconds { get; init; } = 5;

    [Range(1, 10000)]
    public int MatcherBatchSize { get; init; } = 200;

    /// <summary>
    /// Allowed rating (season points) gap at once; grows by <see cref="RatingTolerancePerMinute"/> per waited minute.
    /// </summary>
    [Range(0, long.MaxValue)]
    public long BaseRatingTolerance { get; init; } = 100;

    [Range(0, long.MaxValue)]
    public long RatingTolerancePerMinute { get; init; } = 50;

    /// <summary>
    /// The same two clans meet at most this many times per UTC day.
    /// </summary>
    [Range(1, 100)]
    public int MaxBattlesPerPairPerDay { get; init; } = 3;

    /// <summary>
    /// A battle with no result after this long is cancelled.
    /// </summary>
    [Range(1, 24 * 60)]
    public int BattleTimeoutMinutes { get; init; } = 90;

    [Range(1, 3600)]
    public int RewardIntervalSeconds { get; init; } = 5;

    [Range(1, 10000)]
    public int RewardBatchSize { get; init; } = 500;

    /// <summary>
    /// Concurrent Wallet credits per batch.
    /// </summary>
    [Range(1, 64)]
    public int RewardParallelism { get; init; } = 8;

    [Range(1, 100)]
    public int RewardMaxAttempts { get; init; } = 8;

    [Range(1, 3600)]
    public int RewardBackoffSeconds { get; init; } = 30;

    [Required]
    public string RewardWalletReason { get; init; } = "ClanSeasonReward";

    /// <summary>
    /// Daily reconciliation of delivered grants with Wallet (Quartz cron, UTC).
    /// </summary>
    [Required]
    public string ReconciliationCron { get; init; } = "0 0 3 * * ?";

    [Range(1, 365)]
    public int ReconciliationDays { get; init; } = 7;

    /// <summary>
    /// Items sent to Inventory and not confirmed for this long are sent again.
    /// </summary>
    [Range(1, 24 * 60)]
    public int StuckItemsMinutes { get; init; } = 60;
}
