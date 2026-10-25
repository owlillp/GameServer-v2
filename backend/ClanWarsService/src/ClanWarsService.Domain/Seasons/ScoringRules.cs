namespace ClanWarsService.Domain.Seasons;

/// <summary>
/// Points a clan gets per battle outcome. Stored as jsonb on the season, used by battles (stage 6).
/// </summary>
public sealed record ScoringRules(int WinPoints, int DrawPoints, int LossPoints)
{
    public static ScoringRules Default { get; } = new(WinPoints: 30, DrawPoints: 10, LossPoints: 5);

    public bool IsValid => WinPoints >= 0 && DrawPoints >= 0 && LossPoints >= 0;
}
