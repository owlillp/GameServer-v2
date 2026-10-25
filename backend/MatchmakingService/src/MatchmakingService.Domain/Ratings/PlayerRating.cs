namespace MatchmakingService.Domain.Ratings;

/// <summary>
/// Elo rating of a player for queue matches. Separate from the leaderboard points: the leaderboard
/// rewards activity, the rating measures skill and drives pairing.
/// </summary>
public sealed class PlayerRating
{
    public const int DEFAULT_RATING = 1000;

    public Guid AccountId { get; private set; }

    public int Rating { get; private set; }

    public int Games { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // EF Core
    private PlayerRating() { }

    public PlayerRating(Guid accountId, DateTime now)
    {
        AccountId = accountId;
        Rating = DEFAULT_RATING;
        UpdatedAt = now;
    }

    public void Apply(int delta, DateTime now)
    {
        Rating = Math.Max(0, Rating + delta);
        Games++;
        UpdatedAt = now;
    }
}

/// <summary>
/// Team Elo: each side plays as its average rating; every member gets the side's change.
/// </summary>
public static class Elo
{
    public static double Expected(double rating, double opponentRating) =>
        1.0 / (1.0 + Math.Pow(10, (opponentRating - rating) / 400.0));

    /// <param name="score">1 win, 0.5 draw, 0 loss.</param>
    public static int Delta(double teamRating, double opponentRating, double score, int kFactor) =>
        (int)Math.Round(kFactor * (score - Expected(teamRating, opponentRating)), MidpointRounding.AwayFromZero);

    /// <summary>
    /// Allowed rating gap between two tickets; grows the longer the earlier one waits.
    /// </summary>
    public static int Tolerance(int baseTolerance, int perMinute, TimeSpan waited) =>
        baseTolerance + (perMinute * (int)Math.Max(0, waited.TotalMinutes));
}
