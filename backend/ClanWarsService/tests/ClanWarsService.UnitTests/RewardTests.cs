using ClanWarsService.Domain;
using ClanWarsService.Domain.Rewards;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;
using Xunit;

namespace ClanWarsService.UnitTests;

public sealed class RewardTests
{
    private static readonly DateTime CREATED = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Close_CreatesGrantsOnce_ForRankedClansAndActiveMembers()
    {
        var rewards = new FakeRewards();
        RewardTable table = RewardTable.Create(
            "Default",
            [
                new RewardTierData(1, 1, 100, "coins", 500, [new RewardItem(Guid.NewGuid(), "banner", "COSMETIC", null, 1)], MinBattles: 1),
                new RewardTierData(2, 3, 50, null, 0, [], MinBattles: 0),
            ],
            CREATED).Value;
        await rewards.AddTableAsync(table, CancellationToken.None);

        Season season = Season.CreateDraft(1, "S", CREATED.AddMinutes(5), CREATED.AddHours(1), TimeSpan.Zero, null, table.Id, CREATED).Value;
        season.Publish(CREATED);

        Guid first = Guid.NewGuid();
        Guid fighter = Guid.NewGuid();
        var scores = new FakeScores(
            [
                new SeasonScore(season.Id, first, 90, 3, 3, CREATED),
                new SeasonScore(season.Id, Guid.NewGuid(), 60, 2, 1, CREATED),
                new SeasonScore(season.Id, Guid.NewGuid(), 30, 1, 0, CREATED),
                new SeasonScore(season.Id, Guid.NewGuid(), 10, 1, 0, CREATED),
            ],
            [
                new MemberSeasonStats(season.Id, fighter, first, 90, 3, 3),
                new MemberSeasonStats(season.Id, Guid.NewGuid(), first, 0, 0, 0),
            ]);

        var lifecycle = SeasonLifecycleTests.Create(season, scores, new RecordingPublisher(), new FixedClock(CREATED.AddHours(2)), rewards);

        await lifecycle.AdvanceAsync(season.Id, CancellationToken.None);
        await lifecycle.AdvanceAsync(season.Id, CancellationToken.None);

        Assert.Equal(SeasonStatus.Rewarding, season.Status);
        Assert.Equal(4, rewards.Grants.Count);
        Assert.Equal(3, rewards.Grants.Count(grant => grant.AccountId is null));
        SeasonRewardGrant member = Assert.Single(rewards.Grants, grant => grant.AccountId is not null);
        Assert.Equal(fighter, member.AccountId);
        Assert.Equal(500, member.CurrencyAmount);
        Assert.Single(member.Items);
    }

    [Fact]
    public async Task ThousandClans_ThirtyThousandGrants_RepeatedCloseAddsNothing()
    {
        var rewards = new FakeRewards();
        RewardTable table = RewardTable.Create("Big", [new RewardTierData(1, 1000, 10, "coins", 100, [], MinBattles: 1)], CREATED).Value;
        await rewards.AddTableAsync(table, CancellationToken.None);

        Season season = Season.CreateDraft(1, "S", CREATED.AddMinutes(5), CREATED.AddHours(1), TimeSpan.Zero, null, table.Id, CREATED).Value;
        season.Publish(CREATED);

        var scores = new List<SeasonScore>();
        var members = new List<MemberSeasonStats>();
        for (int clan = 0; clan < 1000; clan++)
        {
            Guid clanId = Guid.NewGuid();
            scores.Add(new SeasonScore(season.Id, clanId, 1000 - clan, 5, 2, CREATED));
            members.AddRange(Enumerable.Range(0, 29).Select(_ => new MemberSeasonStats(season.Id, Guid.NewGuid(), clanId, 10, 2, 1)));
        }

        var lifecycle = SeasonLifecycleTests.Create(season, new FakeScores(scores, members), new RecordingPublisher(), new FixedClock(CREATED.AddHours(2)), rewards);

        await lifecycle.AdvanceAsync(season.Id, CancellationToken.None);
        await lifecycle.AdvanceAsync(season.Id, CancellationToken.None);

        Assert.Equal(30_000, rewards.Grants.Count);
        Assert.Equal(30_000, rewards.Grants.Select(grant => (grant.ClanId, grant.AccountId)).Distinct().Count());
    }

    [Fact]
    public async Task Rewarding_ClosesWhenEveryGrantIsDelivered_NotWhileOneFailed()
    {
        var rewards = new FakeRewards();
        RewardTable table = RewardTable.Create("T", [new RewardTierData(1, 1, 10, null, 0, [], 0)], CREATED).Value;
        await rewards.AddTableAsync(table, CancellationToken.None);

        Season season = Season.CreateDraft(1, "S", CREATED.AddMinutes(5), CREATED.AddHours(1), TimeSpan.Zero, null, table.Id, CREATED).Value;
        season.Publish(CREATED);
        var scores = new FakeScores([new SeasonScore(season.Id, Guid.NewGuid(), 5, 1, 1, CREATED)]);
        DateTime now = CREATED.AddHours(2);
        var lifecycle = SeasonLifecycleTests.Create(season, scores, new RecordingPublisher(), new FixedClock(now), rewards);

        await lifecycle.AdvanceAsync(season.Id, CancellationToken.None);
        SeasonRewardGrant grant = Assert.Single(rewards.Grants);

        grant.Fail("currency disabled");
        Assert.False(await lifecycle.CompleteRewardsAsync(season.Id, CancellationToken.None));

        grant.Retry(now);
        grant.MarkPointsPublished(now);
        Assert.True(await lifecycle.CompleteRewardsAsync(season.Id, CancellationToken.None));
        Assert.Equal(SeasonStatus.Closed, season.Status);
    }

    [Fact]
    public void Grant_RetriesWithBackoff_ThenFails()
    {
        var tier = RewardTable.Create("T", [new RewardTierData(1, 1, 0, "coins", 100, [], 0)], CREATED).Value.Tiers[0];
        SeasonRewardGrant grant = SeasonRewardGrant.ForMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, tier, CREATED);

        grant.RecordFailure("wallet down", CREATED, TimeSpan.FromSeconds(30), maxAttempts: 3);
        Assert.Equal(CREATED.AddSeconds(30), grant.NextAttemptAt);

        grant.RecordFailure("wallet down", CREATED, TimeSpan.FromSeconds(30), maxAttempts: 3);
        Assert.Equal(CREATED.AddSeconds(60), grant.NextAttemptAt);
        Assert.Equal(RewardGrantStatus.Pending, grant.Status);

        grant.RecordFailure("wallet down", CREATED, TimeSpan.FromSeconds(30), maxAttempts: 3);
        Assert.Equal(RewardGrantStatus.Failed, grant.Status);
    }

    [Fact]
    public void Grant_IsGrantedOnlyWhenEveryStepIsDone_AndReopensForReconciliation()
    {
        var tier = RewardTable.Create("T", [new RewardTierData(1, 1, 0, "coins", 100, [new RewardItem(Guid.NewGuid(), "x", "CAR", null, 1)], 0)], CREATED).Value.Tiers[0];
        SeasonRewardGrant grant = SeasonRewardGrant.ForMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, tier, CREATED);

        grant.MarkWalletCredited(CREATED);
        grant.MarkItemsPublished(CREATED);
        Assert.Equal(RewardGrantStatus.Pending, grant.Status);

        grant.MarkItemsGranted(CREATED);
        Assert.Equal(RewardGrantStatus.Granted, grant.Status);

        grant.ReopenWallet(CREATED);
        Assert.Equal(RewardGrantStatus.Pending, grant.Status);
        Assert.True(grant.NeedsWallet);
        Assert.False(grant.NeedsItemsPublish);
    }

    [Fact]
    public void Table_RejectsOverlappingRanks_AndFindsTiers()
    {
        Assert.Equal(
            ClanWarsErrors.RewardTableInvalid,
            RewardTable.Create("T", [new RewardTierData(1, 5, 1, null, 0, [], 0), new RewardTierData(5, 10, 1, null, 0, [], 0)], CREATED).Error);
        Assert.Equal(
            ClanWarsErrors.RewardTableInvalid,
            RewardTable.Create("T", [new RewardTierData(1, 1, 0, null, 10, [], 0)], CREATED).Error);

        RewardTable table = RewardTable.Create("T", [new RewardTierData(11, 100, 1, null, 0, [], 0), new RewardTierData(1, 10, 5, null, 0, [], 0)], CREATED).Value;
        Assert.Equal(5, table.TierFor(3)!.ClanPoints);
        Assert.Equal(1, table.TierFor(100)!.ClanPoints);
        Assert.Null(table.TierFor(101));
    }
}
