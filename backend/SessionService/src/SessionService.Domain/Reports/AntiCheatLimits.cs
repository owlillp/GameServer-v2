namespace SessionService.Domain.Reports;

/// <summary>
/// Plausibility limits for a report. A report over a limit is accepted but flagged for review.
/// </summary>
public sealed record AntiCheatLimits(
    int MinMatchSeconds,
    int MaxMatchMinutes,
    double MaxScorePerMinute,
    double MaxKillsPerMinute);
