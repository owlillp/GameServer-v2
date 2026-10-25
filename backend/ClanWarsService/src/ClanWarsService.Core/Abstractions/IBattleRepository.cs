using ClanWarsService.Domain.Battles;
using ClanWarsService.Domain.Seasons;

namespace ClanWarsService.Core.Abstractions;

public interface IBattleRepository
{
    Task AddWindowsAsync(IReadOnlyCollection<BattleWindow> windows, CancellationToken cancellationToken);

    Task<IReadOnlyList<BattleWindow>> ListWindowsAsync(Guid seasonId, CancellationToken cancellationToken);

    Task<bool> IsWindowOpenAsync(Guid seasonId, DateTime now, CancellationToken cancellationToken);

    Task<BattleQueueEntry?> GetQueuedAsync(Guid clanId, CancellationToken cancellationToken);

    Task AddQueueEntryAsync(BattleQueueEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Queued squads of a season, oldest first, locked with <c>FOR UPDATE SKIP LOCKED</c>.
    /// </summary>
    Task<IReadOnlyList<BattleQueueEntry>> ClaimQueuedAsync(Guid seasonId, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels every queued squad of the season (the season stopped taking new battles).
    /// </summary>
    Task<int> CancelQueuedAsync(Guid seasonId, DateTime now, CancellationToken cancellationToken);

    Task AddBattleAsync(Battle battle, CancellationToken cancellationToken);

    Task<Battle?> GetBattleAsync(Guid battleId, CancellationToken cancellationToken);

    Task<bool> HasOpenBattleAsync(Guid clanId, CancellationToken cancellationToken);

    Task<int> CountBattlesBetweenSinceAsync(Guid clanA, Guid clanB, DateTime since, CancellationToken cancellationToken);

    Task<IReadOnlyList<Battle>> ListByClanAsync(Guid clanId, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Open battles created before <paramref name="createdBefore"/> (null: all open battles of the season).
    /// </summary>
    Task<IReadOnlyList<Guid>> ListOpenAsync(Guid? seasonId, DateTime? createdBefore, int limit, CancellationToken cancellationToken);

    Task<int> CountFinishedBetweenAsync(Guid seasonId, Guid clanA, Guid clanB, CancellationToken cancellationToken);

    Task AddSignalsAsync(IReadOnlyCollection<AbuseSignal> signals, CancellationToken cancellationToken);

    Task<IReadOnlyList<AbuseSignal>> ListSignalsAsync(Guid? seasonId, int limit, CancellationToken cancellationToken);

    /// <summary>Scheduled → InProgress; false if the battle is no longer scheduled.</summary>
    Task<bool> TryStartAsync(Guid battleId, Guid matchId, DateTime now, CancellationToken cancellationToken);

    /// <summary>Scheduled/InProgress → Finished; false if it was already finished or cancelled.</summary>
    Task<bool> TryFinishAsync(Guid battleId, Guid? matchId, BattleOutcome outcome, DateTime now, CancellationToken cancellationToken);

    /// <summary>Scheduled/InProgress → Cancelled; false if it was already finished or cancelled.</summary>
    Task<bool> TryCancelAsync(Guid battleId, string reason, DateTime now, CancellationToken cancellationToken);
}
