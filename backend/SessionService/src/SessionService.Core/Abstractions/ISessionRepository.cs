using SessionService.Domain.Sessions;

namespace SessionService.Core.Abstractions;

public interface ISessionRepository
{
    Task<Session?> GetAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<Session?> GetByMatchAsync(Guid matchId, CancellationToken cancellationToken);

    /// <summary>
    /// The open session the player is currently in, if any.
    /// </summary>
    Task<Session?> GetCurrentByPlayerAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>
    /// Open-world sessions with free slots, fullest first (fill sessions before opening new ones).
    /// </summary>
    Task<IReadOnlyList<Session>> ListJoinableFreeRoamAsync(
        string region,
        string mode,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>
    /// Player slots (sum of max players) reserved by open sessions, per server.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> SumReservedSlotsByServerAsync(
        IReadOnlyCollection<Guid> serverIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Locks open sessions older than <paramref name="createdBefore"/> with <c>FOR UPDATE SKIP LOCKED</c>,
    /// least recently checked first; must run inside a transaction.
    /// </summary>
    Task<IReadOnlyList<Session>> ClaimForLivenessCheckAsync(
        DateTime createdBefore,
        int limit,
        CancellationToken cancellationToken);

    Task AddAsync(Session session, CancellationToken cancellationToken);
}
