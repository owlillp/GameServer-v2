using System.Diagnostics.Metrics;

namespace MatchmakingService.Core;

public static class MatchmakingMetrics
{
    public const string METER_NAME = "MatchmakingService";

    private static readonly Meter METER = new(METER_NAME);

    public static readonly Counter<long> MatchesCreated = METER.CreateCounter<long>(
        "matchmaking_matches_created", description: "Matches paired from the queue or created for external owners");

    public static readonly Counter<long> MatchesCancelled = METER.CreateCounter<long>(
        "matchmaking_matches_cancelled_before_start", description: "Matches cancelled before they started, by reason");

    public static readonly Counter<long> LeaderboardDivergences = METER.CreateCounter<long>(
        "matchmaking_leaderboard_divergences", description: "Times the Redis leaderboard differed from PostgreSQL");
}
