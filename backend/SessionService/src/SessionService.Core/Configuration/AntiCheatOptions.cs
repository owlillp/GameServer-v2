using System.ComponentModel.DataAnnotations;
using SessionService.Domain.Reports;

namespace SessionService.Core.Configuration;

public sealed class AntiCheatOptions
{
    public const string SECTION_NAME = "AntiCheat";

    [Range(0, 3600)]
    public int MinMatchSeconds { get; init; } = 30;

    [Range(1, 24 * 60)]
    public int MaxMatchMinutes { get; init; } = 180;

    [Range(1, double.MaxValue)]
    public double MaxScorePerMinute { get; init; } = 1000;

    [Range(1, double.MaxValue)]
    public double MaxKillsPerMinute { get; init; } = 30;

    public AntiCheatLimits ToLimits() =>
        new(MinMatchSeconds, MaxMatchMinutes, MaxScorePerMinute, MaxKillsPerMinute);
}
