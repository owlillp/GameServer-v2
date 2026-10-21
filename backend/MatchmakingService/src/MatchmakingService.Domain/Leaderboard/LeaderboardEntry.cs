namespace MatchmakingService.Domain.Leaderboard;

public sealed class LeaderboardEntry
{
    public Guid AccountId { get; private set; }

    public long Points { get; private set; }

    public int Wins { get; private set; }

    public int MatchesPlayed { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // EF Core
    private LeaderboardEntry() { }

    public static LeaderboardEntry Create(Guid accountId, long points, bool isWin, DateTime now) =>
        new()
        {
            AccountId = accountId,
            Points = Math.Max(0, points),
            Wins = isWin ? 1 : 0,
            MatchesPlayed = 1,
            UpdatedAt = now,
        };

    public void Apply(long points, bool isWin, DateTime now)
    {
        Points += Math.Max(0, points);
        MatchesPlayed += 1;

        if (isWin)
        {
            Wins += 1;
        }

        UpdatedAt = now;
    }
}
