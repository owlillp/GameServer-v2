using ClanWarsService.Domain.Seasons;

namespace ClanWarsService.Domain.Battles;

/// <summary>
/// Scheduled → InProgress → Finished / Cancelled. Status changes are conditional UPDATEs
/// (<c>WHERE status IN (...)</c>), so a redelivered result or a late cancel cannot change a
/// battle twice.
/// </summary>
public enum BattleStatus
{
    Scheduled = 1,
    InProgress = 2,
    Finished = 3,
    Cancelled = 4,
}

public sealed record BattleOutcome(Guid? WinnerClanId, long PointsA, long PointsB, int WinsA, int WinsB);

public sealed class Battle
{
    public const string MODE = "CLAN-BATTLE";
    public const string EXTERNAL_REF_PREFIX = "clan-battle:";

    public Guid Id { get; private set; }

    public Guid SeasonId { get; private set; }

    public Guid ClanAId { get; private set; }

    public Guid ClanBId { get; private set; }

    public IReadOnlyList<Guid> SquadA { get; private set; } = [];

    public IReadOnlyList<Guid> SquadB { get; private set; } = [];

    public BattleStatus Status { get; private set; }

    // Set only by the conditional SQL transitions of the repository; EF reads them.

    public Guid? MatchId { get; }

    public Guid? WinnerClanId { get; }

    public long PointsA { get; }

    public long PointsB { get; }

    public string? CancelReason { get; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? StartedAt { get; }

    public DateTime? FinishedAt { get; }

    public string ExternalRef => EXTERNAL_REF_PREFIX + Id.ToString("N");

    public bool IsOpen => Status is BattleStatus.Scheduled or BattleStatus.InProgress;

    // EF Core
    private Battle() { }

    public static Battle Create(Guid seasonId, BattleQueueEntry a, BattleQueueEntry b, DateTime now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SeasonId = seasonId,
            ClanAId = a.ClanId,
            ClanBId = b.ClanId,
            SquadA = a.Squad,
            SquadB = b.Squad,
            Status = BattleStatus.Scheduled,
            CreatedAt = now,
        };

    public static bool TryParseExternalRef(string externalRef, out Guid battleId)
    {
        battleId = Guid.Empty;
        return externalRef.StartsWith(EXTERNAL_REF_PREFIX, StringComparison.Ordinal)
            && Guid.TryParseExact(externalRef[EXTERNAL_REF_PREFIX.Length..], "N", out battleId);
    }

    public bool Involves(Guid clanId) => ClanAId == clanId || ClanBId == clanId;

    /// <summary>
    /// Side 1 is clan A, side 2 is clan B; null side is a draw.
    /// </summary>
    public BattleOutcome Score(ScoringRules rules, int? winnerSide) => winnerSide switch
    {
        1 => new BattleOutcome(ClanAId, rules.WinPoints, rules.LossPoints, 1, 0),
        2 => new BattleOutcome(ClanBId, rules.LossPoints, rules.WinPoints, 0, 1),
        _ => new BattleOutcome(null, rules.DrawPoints, rules.DrawPoints, 0, 0),
    };
}
