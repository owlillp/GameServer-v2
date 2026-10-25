namespace MatchmakingService.Domain.Leaderboard;

/// <summary>
/// Read model of a player's standing. Written only by an atomic SQL upsert
/// (<c>points = points + delta</c>), never by EF change tracking, so concurrent match
/// finalizations cannot lose points. <see cref="UpdatedAt"/> is when the current points were reached.
/// </summary>
public sealed class LeaderboardEntry
{
    public Guid AccountId { get; private set; }

    public long Points { get; private set; }

    public int Wins { get; private set; }

    public int MatchesPlayed { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // EF Core
    private LeaderboardEntry() { }

    public LeaderboardEntry(Guid accountId, long points, int wins, int matchesPlayed, DateTime updatedAt)
    {
        AccountId = accountId;
        Points = points;
        Wins = wins;
        MatchesPlayed = matchesPlayed;
        UpdatedAt = updatedAt;
    }
}
