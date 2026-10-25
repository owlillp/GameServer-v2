using ClanWarsService.Domain.Battles;
using ClanWarsService.Domain.Roster;
using ClanWarsService.Domain.Seasons;
using Xunit;

namespace ClanWarsService.UnitTests;

public sealed class AbuseTests
{
    private static readonly DateTime NOW = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly WeakOpponentPolicy POLICY = new(Gap: 300, Scale: 600, MinFactor: 0.25);

    [Theory]
    [InlineData(1000, 900, 30)]
    [InlineData(1000, 700, 30)]
    [InlineData(1000, 400, 15)]
    [InlineData(5000, 0, 8)]
    public void WinOverWeakClan_IsDiscounted(long winner, long loser, long expected)
    {
        Assert.Equal(expected, POLICY.WinPoints(30, winner, loser));
    }

    [Fact]
    public void WeakerSideWinning_GetsFullPoints()
    {
        Battle battle = Battle.Create(Guid.NewGuid(), Entry(rating: 0), Entry(rating: 2000), NOW);

        BattleOutcome outcome = battle.Score(new ScoringRules(30, 10, 5), winnerSide: 1, POLICY);

        Assert.Equal(30, outcome.PointsA);
        Assert.Equal(0, battle.WinnerAdvantage(1));
    }

    [Fact]
    public void Detector_FlagsWeakOpponentRepeatedPairAndNoContest()
    {
        Battle battle = Battle.Create(Guid.NewGuid(), Entry(rating: 1000), Entry(rating: 100), NOW);
        var rules = new AbuseRules(WeakOpponentGap: 300, RepeatedPairThreshold: 5);

        IReadOnlyList<AbuseSignal> signals = AbuseDetector.Check(battle, winnerSide: 1, scoreA: 120, scoreB: 0, pairBattlesInSeason: 10, rules, NOW);

        Assert.Equal(
            [AbuseSignal.WEAK_OPPONENT, AbuseSignal.REPEATED_PAIR, AbuseSignal.NO_CONTEST],
            signals.Select(signal => signal.Kind),
            StringComparer.Ordinal);
        Assert.Empty(AbuseDetector.Check(battle, winnerSide: 2, scoreA: 50, scoreB: 60, pairBattlesInSeason: 3, rules, NOW));
    }

    [Fact]
    public void Roster_RoleChangeKeepsJoinTime_NewClanResetsIt()
    {
        Guid clan = Guid.NewGuid();
        var entry = new RosterEntry(Guid.NewGuid(), clan, "member", NOW);

        entry.Apply(clan, "officer", NOW.AddDays(3));
        Assert.Equal(NOW, entry.JoinedAt);

        entry.Apply(Guid.NewGuid(), "member", NOW.AddDays(5));
        Assert.Equal(NOW.AddDays(5), entry.JoinedAt);
    }

    private static BattleQueueEntry Entry(long rating) =>
        BattleQueueEntry.Create(Guid.Empty, Guid.NewGuid(), [Guid.NewGuid()], 1, 5, rating, Guid.NewGuid(), NOW).Value;
}
