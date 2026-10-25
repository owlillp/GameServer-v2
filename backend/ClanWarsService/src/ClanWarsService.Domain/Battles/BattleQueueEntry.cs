using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace ClanWarsService.Domain.Battles;

public enum BattleQueueStatus
{
    Queued = 1,
    Matched = 2,
    Cancelled = 3,
}

/// <summary>
/// A squad an officer put up for a battle. One queued entry per clan (filtered unique index).
/// </summary>
public sealed class BattleQueueEntry
{
    public Guid Id { get; private set; }

    public Guid SeasonId { get; private set; }

    public Guid ClanId { get; private set; }

    public IReadOnlyList<Guid> Squad { get; private set; } = [];

    /// <summary>
    /// Clan season points when queued: battles are made between clans of similar rating.
    /// </summary>
    public long Rating { get; private set; }

    public Guid QueuedBy { get; private set; }

    public DateTime QueuedAt { get; private set; }

    public BattleQueueStatus Status { get; private set; }

    public Guid? BattleId { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    // EF Core
    private BattleQueueEntry() { }

    public static Result<BattleQueueEntry, Error> Create(
        Guid seasonId,
        Guid clanId,
        IReadOnlyCollection<Guid> squad,
        int minSize,
        int maxSize,
        long rating,
        Guid queuedBy,
        DateTime now)
    {
        Guid[] members = [.. squad.Distinct()];
        if (members.Length != squad.Count || members.Length < minSize || members.Length > maxSize || members.Contains(Guid.Empty))
        {
            return ClanWarsErrors.SquadInvalid;
        }

        return new BattleQueueEntry
        {
            Id = Guid.CreateVersion7(),
            SeasonId = seasonId,
            ClanId = clanId,
            Squad = members,
            Rating = rating,
            QueuedBy = queuedBy,
            QueuedAt = now,
            Status = BattleQueueStatus.Queued,
        };
    }

    public void MarkMatched(Guid battleId, DateTime now)
    {
        Status = BattleQueueStatus.Matched;
        BattleId = battleId;
        ResolvedAt = now;
    }

    public void Cancel(DateTime now)
    {
        if (Status == BattleQueueStatus.Queued)
        {
            Status = BattleQueueStatus.Cancelled;
            ResolvedAt = now;
        }
    }
}
