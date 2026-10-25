using ClanWarsService.Domain;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;
using Xunit;

namespace ClanWarsService.UnitTests;

public sealed class SeasonTests
{
    private static readonly DateTime NOW = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime STARTS = NOW.AddMinutes(10);
    private static readonly DateTime ENDS = STARTS.AddHours(1);
    private static readonly TimeSpan GRACE = TimeSpan.FromMinutes(5);

    [Fact]
    public void Advance_FollowsTheSchedule()
    {
        Season season = Published();

        Assert.Empty(season.Advance(NOW));
        Assert.Equal(SeasonStatus.Scheduled, season.Status);

        Assert.Equal([SeasonStatus.Active], season.Advance(STARTS));
        Assert.True(season.AcceptsScores);

        Assert.Equal([SeasonStatus.Finalizing], season.Advance(ENDS));
        Assert.True(season.AcceptsScores);
        Assert.False(season.IsReadyToClose(ENDS));
        Assert.True(season.IsReadyToClose(ENDS + GRACE));
    }

    [Fact]
    public void Advance_AfterDowntime_CatchesUpInOneCall_AndIsIdempotent()
    {
        Season season = Published();
        DateTime backOnline = ENDS.AddHours(3);

        Assert.Equal([SeasonStatus.Active, SeasonStatus.Finalizing], season.Advance(backOnline));
        Assert.Empty(season.Advance(backOnline));
        Assert.True(season.IsReadyToClose(backOnline));
    }

    [Fact]
    public void Close_WithoutRewardTable_Closes_WithTable_WaitsForRewards()
    {
        Season free = Published();
        free.Advance(ENDS + GRACE);
        Assert.Equal(SeasonStatus.Closed, free.Close(ENDS + GRACE));
        Assert.False(free.AcceptsScores);
        Assert.Equal(SeasonStatus.Closed, free.Close(ENDS + GRACE));

        Season rewarded = Published(rewardTableId: Guid.NewGuid());
        rewarded.Advance(ENDS + GRACE);
        Assert.Equal(SeasonStatus.Rewarding, rewarded.Close(ENDS + GRACE));
        Assert.Empty(rewarded.Advance(ENDS.AddDays(1)));
    }

    [Fact]
    public void Draft_IsNotDrivenByTheClock()
    {
        Season draft = Draft();

        Assert.Empty(draft.Advance(ENDS.AddDays(1)));
        Assert.Equal(SeasonStatus.Draft, draft.Status);
        Assert.False(draft.AcceptsScores);
    }

    [Fact]
    public void Publish_OnlyOnce_AndUpdateOnlyInDraft()
    {
        Season season = Draft();

        Assert.True(season.Publish(NOW).IsSuccess);
        Assert.Equal(ClanWarsErrors.SeasonNotDraft, season.Publish(NOW).Error);
        Assert.Equal(
            ClanWarsErrors.SeasonNotDraft,
            season.UpdateDraft("x", STARTS, ENDS, GRACE, null, null, NOW).Error);
    }

    [Fact]
    public void CreateDraft_ValidatesScheduleAndRules()
    {
        Assert.Equal(ClanWarsErrors.SeasonScheduleInvalid, Season.CreateDraft(1, "S", ENDS, STARTS, GRACE, null, null, NOW).Error);
        Assert.Equal(ClanWarsErrors.SeasonInPast, Season.CreateDraft(1, "S", NOW.AddHours(-2), NOW.AddHours(-1), GRACE, null, null, NOW).Error);
        Assert.Equal(ClanWarsErrors.ScoringRulesInvalid, Season.CreateDraft(1, "S", STARTS, ENDS, GRACE, new ScoringRules(-1, 0, 0), null, NOW).Error);
        Assert.Equal(ClanWarsErrors.SeasonNameInvalid, Season.CreateDraft(1, " ", STARTS, ENDS, GRACE, null, null, NOW).Error);

        Season season = Draft();
        Assert.Equal(ENDS + GRACE, season.FinalizesAt);
        Assert.Equal(ScoringRules.Default, season.ScoringRules);
    }

    [Fact]
    public void ScoreReceipt_RequiresAmountSourceAndReason()
    {
        Assert.Equal(ClanWarsErrors.ScoreInvalid, ScoreReceipt.Create(Guid.NewGuid(), Guid.NewGuid(), "battle:1", 0, "win", null, NOW).Error);
        Assert.Equal(ClanWarsErrors.ScoreInvalid, ScoreReceipt.Create(Guid.NewGuid(), Guid.NewGuid(), " ", 10, "win", null, NOW).Error);
        Assert.True(ScoreReceipt.Create(Guid.NewGuid(), Guid.NewGuid(), "admin:1", -10, "penalty", Guid.NewGuid(), NOW).IsSuccess);
    }

    [Fact]
    public void LeaderboardScore_TiesGoToWhoReachedFirst()
    {
        Assert.True(LeaderboardScore.Encode(100, NOW) > LeaderboardScore.Encode(100, NOW.AddMinutes(1)));
        Assert.True(LeaderboardScore.Encode(101, NOW.AddDays(30)) > LeaderboardScore.Encode(100, NOW));
        Assert.Equal(12345, LeaderboardScore.DecodePoints(LeaderboardScore.Encode(12345, NOW)));
    }

    private static Season Draft(Guid? rewardTableId = null) =>
        Season.CreateDraft(1, "Season 1", STARTS, ENDS, GRACE, null, rewardTableId, NOW).Value;

    private static Season Published(Guid? rewardTableId = null)
    {
        Season season = Draft(rewardTableId);
        season.Publish(NOW);
        return season;
    }
}
