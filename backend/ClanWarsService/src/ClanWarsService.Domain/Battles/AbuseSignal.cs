namespace ClanWarsService.Domain.Battles;

/// <summary>
/// Something that looks like abuse, kept for review and sent to the future Moderation service.
/// Signals never change scores by themselves.
/// </summary>
public sealed class AbuseSignal
{
    public const string WEAK_OPPONENT = "weak-opponent";
    public const string REPEATED_PAIR = "repeated-pair";
    public const string NO_CONTEST = "no-contest";

    public Guid Id { get; private set; }

    public string Kind { get; private set; } = null!;

    public Guid SeasonId { get; private set; }

    public IReadOnlyList<Guid> ClanIds { get; private set; } = [];

    public Guid? BattleId { get; private set; }

    public string Details { get; private set; } = null!;

    public DateTime DetectedAt { get; private set; }

    // EF Core
    private AbuseSignal() { }

    public AbuseSignal(string kind, Guid seasonId, IReadOnlyList<Guid> clanIds, Guid? battleId, string details, DateTime detectedAt)
    {
        Id = Guid.CreateVersion7();
        Kind = kind;
        SeasonId = seasonId;
        ClanIds = clanIds;
        BattleId = battleId;
        Details = details.Length > 512 ? details[..512] : details;
        DetectedAt = detectedAt;
    }
}

public sealed record AbuseRules(long WeakOpponentGap, int RepeatedPairThreshold);

/// <summary>
/// Checks a finished battle: a big rating gap won by the stronger clan, the same pair meeting too
/// often in a season, a losing side that did not score at all (thrown battle).
/// </summary>
public static class AbuseDetector
{
    public static IReadOnlyList<AbuseSignal> Check(
        Battle battle,
        int? winnerSide,
        long scoreA,
        long scoreB,
        int pairBattlesInSeason,
        AbuseRules rules,
        DateTime now)
    {
        var signals = new List<AbuseSignal>();
        Guid[] clans = [battle.ClanAId, battle.ClanBId];

        long advantage = battle.WinnerAdvantage(winnerSide);
        if (advantage > rules.WeakOpponentGap)
        {
            signals.Add(new AbuseSignal(AbuseSignal.WEAK_OPPONENT, battle.SeasonId, clans, battle.Id, $"winner rating advantage {advantage}", now));
        }

        if (pairBattlesInSeason > 0 && pairBattlesInSeason % rules.RepeatedPairThreshold == 0)
        {
            signals.Add(new AbuseSignal(AbuseSignal.REPEATED_PAIR, battle.SeasonId, clans, battle.Id, $"{pairBattlesInSeason} battles between these clans this season", now));
        }

        long loserScore = winnerSide switch { 1 => scoreB, 2 => scoreA, _ => -1 };
        if (loserScore == 0)
        {
            signals.Add(new AbuseSignal(AbuseSignal.NO_CONTEST, battle.SeasonId, clans, battle.Id, "losing side scored nothing", now));
        }

        return signals;
    }
}
