using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.Domain.Scores;

/// <summary>
/// Proof that a score source (a battle, an admin correction) was applied to a clan in a season.
/// Unique by (season, clan, source): a repeated source changes nothing.
/// </summary>
public sealed class ScoreReceipt
{
    public const int SOURCE_MAX_LENGTH = 128;
    public const int REASON_MAX_LENGTH = 256;

    public Guid Id { get; private set; }

    public Guid SeasonId { get; private set; }

    public Guid ClanId { get; private set; }

    public string SourceId { get; private set; } = null!;

    public long Amount { get; private set; }

    public string Reason { get; private set; } = null!;

    public Guid? ActorId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // EF Core
    private ScoreReceipt() { }

    public static Result<ScoreReceipt, Error> Create(
        Guid seasonId,
        Guid clanId,
        string sourceId,
        long amount,
        string reason,
        Guid? actorId,
        DateTime now)
    {
        string source = (sourceId ?? string.Empty).Trim();
        string trimmedReason = (reason ?? string.Empty).Trim();

        if (amount == 0
            || source.Length is 0 or > SOURCE_MAX_LENGTH
            || trimmedReason.Length is 0 or > REASON_MAX_LENGTH)
        {
            return ClanWarsErrors.ScoreInvalid;
        }

        return new ScoreReceipt
        {
            Id = Guid.CreateVersion7(),
            SeasonId = seasonId,
            ClanId = clanId,
            SourceId = source,
            Amount = amount,
            Reason = trimmedReason,
            ActorId = actorId,
            CreatedAt = now,
        };
    }
}

/// <summary>
/// Read model of a clan's season standing. Written only by an atomic SQL upsert
/// (<c>points = points + delta</c>); <see cref="UpdatedAt"/> is when the current points were reached.
/// </summary>
public sealed class SeasonScore
{
    public Guid SeasonId { get; private set; }

    public Guid ClanId { get; private set; }

    public long Points { get; private set; }

    public int Battles { get; private set; }

    public int Wins { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // EF Core
    private SeasonScore() { }

    public SeasonScore(Guid seasonId, Guid clanId, long points, int battles, int wins, DateTime updatedAt)
    {
        SeasonId = seasonId;
        ClanId = clanId;
        Points = points;
        Battles = battles;
        Wins = wins;
        UpdatedAt = updatedAt;
    }
}

/// <summary>
/// A player's contribution to the clan in a season (filled by battles, stage 6).
/// </summary>
public sealed class MemberSeasonStats
{
    public Guid SeasonId { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid ClanId { get; private set; }

    public long Points { get; private set; }

    public int Battles { get; private set; }

    public int Wins { get; private set; }

    // EF Core
    private MemberSeasonStats() { }

    public MemberSeasonStats(Guid seasonId, Guid accountId, Guid clanId, long points, int battles, int wins)
    {
        SeasonId = seasonId;
        AccountId = accountId;
        ClanId = clanId;
        Points = points;
        Battles = battles;
        Wins = wins;
    }
}

/// <summary>
/// Frozen final standings, taken when the season closes; rewards (stage 7) are based on it.
/// </summary>
public sealed class SeasonResult
{
    public Guid SeasonId { get; private set; }

    public Guid ClanId { get; private set; }

    public int Rank { get; private set; }

    public long Points { get; private set; }

    public int Battles { get; private set; }

    public int Wins { get; private set; }

    // EF Core
    private SeasonResult() { }

    public SeasonResult(Guid seasonId, int rank, SeasonScore score)
    {
        SeasonId = seasonId;
        ClanId = score.ClanId;
        Rank = rank;
        Points = score.Points;
        Battles = score.Battles;
        Wins = score.Wins;
    }
}
