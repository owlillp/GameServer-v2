using MatchmakingService.Domain.Leaderboard;
using Xunit;

namespace MatchmakingService.UnitTests;

public sealed class LeaderboardScoreTests
{
    private static readonly DateTime NOW = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MorePoints_RankHigher_RegardlessOfTime()
    {
        double earlierFewer = LeaderboardScore.Encode(100, NOW.AddYears(-1));
        double laterMore = LeaderboardScore.Encode(101, NOW);

        Assert.True(laterMore > earlierFewer);
    }

    [Fact]
    public void EqualPoints_EarlierAchievement_RanksHigher()
    {
        double earlier = LeaderboardScore.Encode(500, NOW);
        double later = LeaderboardScore.Encode(500, NOW.AddMinutes(1));

        Assert.True(earlier > later);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(123_456)]
    [InlineData(LeaderboardScore.MAX_POINTS)]
    public void DecodePoints_RoundTrips(long points)
    {
        double score = LeaderboardScore.Encode(points, NOW);

        Assert.Equal(points, LeaderboardScore.DecodePoints(score));
    }

    [Fact]
    public void Encode_StaysExactInDouble()
    {
        double score = LeaderboardScore.Encode(LeaderboardScore.MAX_POINTS, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.True(score < Math.Pow(2, 53));
        Assert.Equal(score, Math.Floor(score));
    }
}
