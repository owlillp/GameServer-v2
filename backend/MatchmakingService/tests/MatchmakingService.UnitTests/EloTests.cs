using MatchmakingService.Domain.Ratings;
using Xunit;

namespace MatchmakingService.UnitTests;

public sealed class EloTests
{
    [Fact]
    public void EqualTeams_WinnerGainsHalfK()
    {
        Assert.Equal(16, Elo.Delta(1000, 1000, 1, 32));
        Assert.Equal(-16, Elo.Delta(1000, 1000, 0, 32));
        Assert.Equal(0, Elo.Delta(1000, 1000, 0.5, 32));
    }

    [Fact]
    public void Upset_IsWorthMoreThanExpectedWin()
    {
        int favouriteWins = Elo.Delta(1400, 1000, 1, 32);
        int underdogWins = Elo.Delta(1000, 1400, 1, 32);

        Assert.True(underdogWins > favouriteWins);
        Assert.Equal(32, favouriteWins + underdogWins);
    }

    [Fact]
    public void Tolerance_WidensWithWaiting()
    {
        Assert.Equal(100, Elo.Tolerance(100, 50, TimeSpan.FromSeconds(30)));
        Assert.Equal(350, Elo.Tolerance(100, 50, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void Rating_NeverGoesBelowZero()
    {
        var rating = new PlayerRating(Guid.NewGuid(), DateTime.UtcNow);

        rating.Apply(-5000, DateTime.UtcNow);

        Assert.Equal(0, rating.Rating);
        Assert.Equal(1, rating.Games);
    }
}
