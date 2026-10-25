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
}
