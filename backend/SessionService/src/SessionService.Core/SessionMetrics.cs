using System.Diagnostics.Metrics;

namespace SessionService.Core;

public static class SessionMetrics
{
    public const string METER_NAME = "SessionService";

    private static readonly Meter METER = new(METER_NAME);

    public static readonly Counter<long> Reports = METER.CreateCounter<long>(
        "session_match_reports", description: "Match reports from game servers, by result");

    public static readonly Counter<long> SessionsExpired = METER.CreateCounter<long>(
        "session_sessions_expired", description: "Sessions closed because their server stopped reporting them");

    public static readonly Counter<long> Joins = METER.CreateCounter<long>(
        "session_join_tickets", description: "Join tickets issued to players");

    public static void Report(string result) => Reports.Add(1, new KeyValuePair<string, object?>("result", result));
}
