using System.Diagnostics.Metrics;

namespace ClanWarsService.Core;

/// <summary>
/// Event-driven instruments; gauges sampled from the database live in the web project.
/// </summary>
public static class ClanWarsMetrics
{
    public const string METER_NAME = "ClanWarsService";

    private static readonly Meter METER = new(METER_NAME);

    /// <summary>
    /// Time from a clan event to its application to the roster projection.
    /// </summary>
    public static readonly Histogram<double> RosterEventLag = METER.CreateHistogram<double>(
        "clanwars_roster_event_lag", "s", "Delay between a ClanService event and the roster projection update");

    public static readonly Counter<long> LeaderboardDivergences = METER.CreateCounter<long>(
        "clanwars_leaderboard_divergences", description: "Times the Redis leaderboard differed from PostgreSQL");

    public static readonly Counter<long> LeaderboardRebuilds = METER.CreateCounter<long>(
        "clanwars_leaderboard_rebuilds", description: "Leaderboard rebuilds from PostgreSQL");

    public static readonly Counter<long> BattlesFinished = METER.CreateCounter<long>(
        "clanwars_battles_finished", description: "Battles that ended, by status");

    public static readonly Counter<long> AbuseSignals = METER.CreateCounter<long>(
        "clanwars_abuse_signals", description: "Abuse signals raised, by kind");

    public static void RecordLag(DateTime eventAt, DateTime now) =>
        RosterEventLag.Record(Math.Max(0, (now - eventAt).TotalSeconds));
}
