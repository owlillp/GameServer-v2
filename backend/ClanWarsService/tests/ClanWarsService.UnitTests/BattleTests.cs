using ClanWarsService.Core.Services;
using ClanWarsService.Domain;
using ClanWarsService.Domain.Battles;
using ClanWarsService.Domain.Seasons;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClanWarsService.UnitTests;

public sealed class BattleTests
{
    private static readonly DateTime NOW = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ResultDeliveredTwice_CreditsPointsOnce()
    {
        (Season season, Battle battle, FakeBattles battles, FakeScores scores, RecordingPublisher publisher, BattleResultService service) = Setup();

        await service.ApplyResultAsync(battle.Id, Guid.NewGuid(), winnerSide: 1, CancellationToken.None);
        await service.ApplyResultAsync(battle.Id, Guid.NewGuid(), winnerSide: 1, CancellationToken.None);

        Assert.Equal(BattleStatus.Finished, battles.StatusOf(battle.Id));
        Assert.Equal(season.ScoringRules.WinPoints, scores.Points[battle.ClanAId]);
        Assert.Equal(season.ScoringRules.LossPoints, scores.Points[battle.ClanBId]);
        Assert.Equal(2, scores.Receipts.Count);
        Assert.All(battle.SquadA.Concat(battle.SquadB), member => Assert.Equal(1, scores.MemberBattles[member]));
        Assert.Single(publisher.BattlesFinished);
    }

    [Fact]
    public async Task ResultAfterCancel_GivesNoPoints()
    {
        (_, Battle battle, FakeBattles battles, FakeScores scores, _, BattleResultService service) = Setup();

        await service.CancelAsync(battle.Id, "timeout", CancellationToken.None);
        await service.ApplyResultAsync(battle.Id, Guid.NewGuid(), winnerSide: 2, CancellationToken.None);

        Assert.Equal(BattleStatus.Cancelled, battles.StatusOf(battle.Id));
        Assert.Empty(scores.Receipts);
    }

    [Fact]
    public void Score_DrawGivesDrawPointsToBoth()
    {
        Battle battle = NewBattle(Guid.NewGuid());
        BattleOutcome outcome = battle.Score(new ScoringRules(30, 10, 5), winnerSide: null);

        Assert.Null(outcome.WinnerClanId);
        Assert.Equal((10, 10), (outcome.PointsA, outcome.PointsB));
        Assert.Equal((0, 0), (outcome.WinsA, outcome.WinsB));
    }

    [Fact]
    public void ExternalRef_RoundTrips()
    {
        Battle battle = NewBattle(Guid.NewGuid());

        Assert.True(Battle.TryParseExternalRef(battle.ExternalRef, out Guid parsed));
        Assert.Equal(battle.Id, parsed);
        Assert.False(Battle.TryParseExternalRef("match:123", out _));
    }

    [Fact]
    public void Pairing_ToleranceGrowsWithWaiting_AndNeedsSameSquadSize()
    {
        var rules = new PairingRules(BaseRatingTolerance: 100, RatingTolerancePerMinute: 50);
        Guid season = Guid.NewGuid();
        BattleQueueEntry low = Entry(season, rating: 0, squad: 2, queuedAt: NOW);
        BattleQueueEntry high = Entry(season, rating: 300, squad: 2, queuedAt: NOW);
        BattleQueueEntry bigSquad = Entry(season, rating: 0, squad: 3, queuedAt: NOW);

        Assert.False(BattlePairing.CanPair(low, high, rules, NOW));
        Assert.True(BattlePairing.CanPair(low, high, rules, NOW.AddMinutes(4)));
        Assert.False(BattlePairing.CanPair(low, bigSquad, rules, NOW.AddHours(1)));
        Assert.False(BattlePairing.CanPair(low, low, rules, NOW));
    }

    [Fact]
    public void QueueEntry_RejectsDuplicatesAndSize()
    {
        Guid player = Guid.NewGuid();

        Assert.Equal(ClanWarsErrors.SquadInvalid, BattleQueueEntry.Create(Guid.NewGuid(), Guid.NewGuid(), [player, player], 1, 5, 0, player, NOW).Error);
        Assert.Equal(ClanWarsErrors.SquadInvalid, BattleQueueEntry.Create(Guid.NewGuid(), Guid.NewGuid(), [], 1, 5, 0, player, NOW).Error);
    }

    [Fact]
    public void BattleWindows_FromDailyTemplate_AreCutToTheSeason()
    {
        DateTime starts = new(2026, 11, 1, 20, 0, 0, DateTimeKind.Utc);
        DateTime ends = new(2026, 11, 3, 21, 0, 0, DateTimeKind.Utc);
        Season season = Season.CreateDraft(1, "S", starts, ends, TimeSpan.Zero, null, null, NOW, new BattleWindowTemplate(new TimeOnly(19, 0), new TimeOnly(23, 0))).Value;

        IReadOnlyList<(DateTime StartsAt, DateTime EndsAt)> windows = season.BattleWindows();

        Assert.Equal(
            [(starts, starts.Date.AddHours(23)), (starts.Date.AddDays(1).AddHours(19), starts.Date.AddDays(1).AddHours(23)), (ends.Date.AddHours(19), ends)],
            windows);
    }

    [Fact]
    public void BattleWindows_PastMidnight()
    {
        DateTime starts = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        Season season = Season.CreateDraft(1, "S", starts, starts.AddDays(1), TimeSpan.Zero, null, null, NOW, new BattleWindowTemplate(new TimeOnly(22, 0), new TimeOnly(2, 0))).Value;

        Assert.Equal(
            [(starts, starts.AddHours(2)), (starts.AddHours(22), starts.AddDays(1))],
            season.BattleWindows());
    }

    private static (Season, Battle, FakeBattles, FakeScores, RecordingPublisher, BattleResultService) Setup()
    {
        Season season = Season.CreateDraft(1, "S", NOW.AddMinutes(-10), NOW.AddHours(1), TimeSpan.Zero, null, null, NOW.AddMinutes(-20)).Value;
        season.Publish(NOW.AddMinutes(-20));
        season.Advance(NOW);

        Battle battle = NewBattle(season.Id);
        var battles = new FakeBattles();
        battles.Add(battle);

        var scores = new FakeScores([]);
        var publisher = new RecordingPublisher();
        var clock = new FixedClock(NOW);
        var seasons = new FakeSeasons(season);
        var transactions = new NoTransaction();

        var scoreService = new ScoreService(
            seasons,
            new NoRoster(),
            scores,
            new NoopLeaderboard(),
            new LeaderboardSyncSignal(),
            transactions,
            clock,
            NullLogger<ScoreService>.Instance);

        var service = new BattleResultService(battles, seasons, scoreService, publisher, transactions, new StaticOptions(), clock, NullLogger<BattleResultService>.Instance);

        return (season, battle, battles, scores, publisher, service);
    }

    private static Battle NewBattle(Guid seasonId) =>
        Battle.Create(seasonId, Entry(seasonId, 0, 2, NOW), Entry(seasonId, 0, 2, NOW), NOW);

    private static BattleQueueEntry Entry(Guid seasonId, long rating, int squad, DateTime queuedAt) =>
        BattleQueueEntry.Create(
            seasonId,
            Guid.NewGuid(),
            [.. Enumerable.Range(0, squad).Select(_ => Guid.NewGuid())],
            1,
            5,
            rating,
            Guid.NewGuid(),
            queuedAt).Value;
}
