using MatchmakingService.Domain.Queues;

namespace MatchmakingService.Core.Abstractions;

public interface IQueueRepository
{
    Task<QueueTicket?> GetActiveByPartyAsync(Guid partyId, CancellationToken cancellationToken);

    Task<QueueTicket?> GetAsync(Guid ticketId, CancellationToken cancellationToken);

    Task<IReadOnlyList<QueueTicket>> ListQueuedAsync(int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Locks queued tickets with <c>FOR UPDATE SKIP LOCKED</c>; must run inside a transaction.
    /// Tickets locked by another replica are skipped instead of being paired twice.
    /// </summary>
    Task<IReadOnlyList<QueueTicket>> ClaimQueuedAsync(int limit, CancellationToken cancellationToken);

    Task AddAsync(QueueTicket ticket, CancellationToken cancellationToken);
}
