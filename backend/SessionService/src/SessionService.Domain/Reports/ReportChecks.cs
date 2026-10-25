using CSharpFunctionalExtensions;
using SessionService.Domain.Sessions;
using Shared.SharedKernel.Errors;

namespace SessionService.Domain.Reports;

public static class ReportChecks
{
    public const string FLAG_TOO_SHORT = "duration.too_short";
    public const string FLAG_TOO_LONG = "duration.too_long";
    public const string FLAG_SCORE_RATE = "score.rate";
    public const string FLAG_KILL_RATE = "kills.rate";

    /// <summary>
    /// Hard checks: the report is rejected if it fails them.
    /// </summary>
    public static UnitResult<Error> Validate(MatchReport report, Session session)
    {
        if (string.IsNullOrWhiteSpace(report.ActivityId)
            || report.ActivityId.Length > SessionConstants.ACTIVITY_ID_MAX_LENGTH
            || report.Players.Count is 0 or > SessionConstants.MAX_REPORT_PLAYERS
            || report.EndedAt < report.StartedAt
            || (report.StatsJson?.Length ?? 0) > SessionConstants.MAX_STATS_JSON_LENGTH)
        {
            return SessionErrors.ReportInvalid;
        }

        var seen = new HashSet<Guid>();

        foreach (ReportedPlayer player in report.Players)
        {
            if (!seen.Add(player.AccountId)
                || player.Score < 0
                || player.Kills < 0
                || player.Deaths < 0)
            {
                return SessionErrors.ReportInvalid;
            }

            if (!session.HasPlayed(player.AccountId))
            {
                return SessionErrors.ReportPlayerUnknown;
            }
        }

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Soft checks: suspicious values become flags on the stored raw report.
    /// </summary>
    public static IReadOnlyList<string> Flags(MatchReport report, AntiCheatLimits limits)
    {
        var flags = new List<string>();
        TimeSpan duration = report.Duration;

        if (duration.TotalSeconds < limits.MinMatchSeconds)
        {
            flags.Add(FLAG_TOO_SHORT);
        }

        if (duration.TotalMinutes > limits.MaxMatchMinutes)
        {
            flags.Add(FLAG_TOO_LONG);
        }

        double minutes = Math.Max(duration.TotalMinutes, 1.0 / 60);

        foreach (ReportedPlayer player in report.Players)
        {
            if (player.Score / minutes > limits.MaxScorePerMinute)
            {
                flags.Add($"{FLAG_SCORE_RATE}:{player.AccountId}");
            }

            if (player.Kills / minutes > limits.MaxKillsPerMinute)
            {
                flags.Add($"{FLAG_KILL_RATE}:{player.AccountId}");
            }
        }

        return flags;
    }
}
