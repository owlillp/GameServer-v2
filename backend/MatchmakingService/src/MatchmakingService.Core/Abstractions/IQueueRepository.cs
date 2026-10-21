using MatchmakingService.Domain.Queues;

namespace MatchmakingService.Core.Abstractions;

public interface IQueueRepository
{
    Task<QueueTicket?> GetActiveByPartyAsync(Guid partyId, CancellationToken cancellationToken);

    Task<QueueTicket?> GetAsync(Guid ticketId, CancellationToken cancellationToken);

    Task<IReadOnlyList<QueueTicket>> ListQueuedAsync(int limit, CancellationToken cancellationToken);

    Task AddAsync(QueueTicket ticket, CancellationToken cancellationToken);
}
