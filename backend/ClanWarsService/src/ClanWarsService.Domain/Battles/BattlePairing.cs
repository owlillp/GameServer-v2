namespace ClanWarsService.Domain.Battles;

public sealed record PairingRules(long BaseRatingTolerance, long RatingTolerancePerMinute);

/// <summary>
/// Who may fight whom: other clan, same squad size, rating gap within a tolerance that grows the
/// longer either squad waits. The daily repeat limit needs the database and is checked by the matcher.
/// </summary>
public static class BattlePairing
{
    public static long Tolerance(BattleQueueEntry a, BattleQueueEntry b, PairingRules rules, DateTime now)
    {
        DateTime earliest = a.QueuedAt < b.QueuedAt ? a.QueuedAt : b.QueuedAt;
        long waitedMinutes = Math.Max(0, (long)(now - earliest).TotalMinutes);

        return rules.BaseRatingTolerance + (rules.RatingTolerancePerMinute * waitedMinutes);
    }

    public static bool CanPair(BattleQueueEntry a, BattleQueueEntry b, PairingRules rules, DateTime now) =>
        a.ClanId != b.ClanId
        && a.SeasonId == b.SeasonId
        && a.Squad.Count == b.Squad.Count
        && Math.Abs(a.Rating - b.Rating) <= Tolerance(a, b, rules, now);
}
