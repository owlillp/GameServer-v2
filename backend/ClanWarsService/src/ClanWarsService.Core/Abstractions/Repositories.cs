using ClanWarsService.Domain.Roster;
using ClanWarsService.Domain.Scores;
using ClanWarsService.Domain.Seasons;

namespace ClanWarsService.Core.Abstractions;

public interface ISeasonRepository
{
    Task<Season?> GetAsync(Guid seasonId, CancellationToken cancellationToken);

    /// <summary>
    /// Waits for the row lock; must run inside a transaction. Serialises phase jobs and score writes.
    /// </summary>
    Task<Season?> GetForUpdateAsync(Guid seasonId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Season>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Published seasons that are not closed yet: they need phase triggers.
    /// </summary>
    Task<IReadOnlyList<Season>> ListOpenAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The running season (Active, Finalizing, Rewarding) or else the next scheduled one.
    /// </summary>
    Task<Season?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<int> NextNumberAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Season>> ListByStatusAsync(SeasonStatus status, CancellationToken cancellationToken);

    /// <summary>
    /// True if another published season overlaps [startsAt, finalizesAt].
    /// </summary>
    Task<bool> OverlapsAsync(Guid seasonId, DateTime startsAt, DateTime finalizesAt, CancellationToken cancellationToken);

    Task AddAsync(Season season, CancellationToken cancellationToken);
}

public interface IRosterRepository
{
    Task<ClanProjection?> GetClanAsync(Guid clanId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClanProjection>> ListClansAsync(IReadOnlyCollection<Guid> clanIds, CancellationToken cancellationToken);

    Task AddClanAsync(ClanProjection clan, CancellationToken cancellationToken);

    Task<RosterEntry?> GetEntryAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RosterEntry>> ListEntriesAsync(Guid clanId, CancellationToken cancellationToken);

    Task AddEntryAsync(RosterEntry entry, CancellationToken cancellationToken);

    void RemoveEntry(RosterEntry entry);
}

public interface IScoreRepository
{
    Task AddReceiptAsync(ScoreReceipt receipt, CancellationToken cancellationToken);

    Task<IReadOnlyList<ScoreReceipt>> ListReceiptsAsync(Guid seasonId, Guid? clanId, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Atomic upsert <c>points = points + delta</c> in the current transaction.
    /// </summary>
    Task ApplyAsync(Guid seasonId, Guid clanId, long delta, int battles, int wins, DateTime now, CancellationToken cancellationToken);

    Task<SeasonScore?> GetAsync(Guid seasonId, Guid clanId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SeasonScore>> ListByClansAsync(Guid seasonId, IReadOnlyCollection<Guid> clanIds, CancellationToken cancellationToken);

    /// <summary>
    /// Ordered by points, ties by who reached them first, then clan id (same order as Redis).
    /// </summary>
    Task<IReadOnlyList<SeasonScore>> ListTopAsync(Guid seasonId, int offset, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Keyset page by clan id: stable while points change, used for rebuilds.
    /// </summary>
    Task<IReadOnlyList<SeasonScore>> ListAfterAsync(Guid seasonId, Guid? afterClanId, int limit, CancellationToken cancellationToken);

    Task<int> CountAsync(Guid seasonId, CancellationToken cancellationToken);

    Task<int> CountAheadOfAsync(SeasonScore score, CancellationToken cancellationToken);

    Task<bool> ReceiptExistsAsync(Guid seasonId, Guid clanId, string sourceId, CancellationToken cancellationToken);

    /// <summary>
    /// Atomic upsert of a player's contribution in the current transaction.
    /// </summary>
    Task ApplyMemberAsync(Guid seasonId, Guid accountId, Guid clanId, long points, int battles, int wins, CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberSeasonStats>> ListMemberStatsBySeasonAsync(Guid seasonId, int minBattles, CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberSeasonStats>> ListMemberStatsAsync(Guid seasonId, Guid clanId, CancellationToken cancellationToken);

    Task AddResultsAsync(IReadOnlyCollection<SeasonResult> results, CancellationToken cancellationToken);

    /// <summary>
    /// <c>pg_try_advisory_lock</c> per season; null when another replica rebuilds it.
    /// </summary>
    Task<IAsyncDisposable?> TryAcquireRebuildLockAsync(Guid seasonId, CancellationToken cancellationToken);
}
