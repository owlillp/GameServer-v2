namespace ClanWarsService.Domain;

/// <summary>
/// Composite Redis score: points in the high bits, "earlier is better" time in the low bits,
/// so clans with equal points are ordered by who reached them first. Stays below 2^53.
/// Same encoding as the matchmaking leaderboard.
/// </summary>
public static class LeaderboardScore
{
    public const long MAX_POINTS = (1L << 27) - 1;

    private const long TIME_SLOTS = 1L << 26;

    private static readonly DateTime EPOCH = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static double Encode(long points, DateTime achievedAt)
    {
        long minutes = Math.Clamp((long)(achievedAt - EPOCH).TotalMinutes, 0, TIME_SLOTS - 1);
        long clampedPoints = Math.Clamp(points, 0, MAX_POINTS);

        return (clampedPoints * TIME_SLOTS) + (TIME_SLOTS - 1 - minutes);
    }

    public static long DecodePoints(double score) => (long)Math.Floor(score / TIME_SLOTS);
}
