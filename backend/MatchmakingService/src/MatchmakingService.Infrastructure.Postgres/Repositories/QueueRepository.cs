using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Queues;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingService.Infrastructure.Postgres.Repositories;

public sealed class QueueRepository(MatchmakingServiceDbContext dbContext) : IQueueRepository
{
    public Task<QueueTicket?> GetActiveByPartyAsync(Guid partyId, CancellationToken cancellationToken) =>
        dbContext.QueueTickets
            .Where(ticket => ticket.PartyId == partyId && ticket.Status == QueueTicketStatus.Queued)
            .OrderByDescending(ticket => ticket.EnqueuedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<QueueTicket?> GetAsync(Guid ticketId, CancellationToken cancellationToken) =>
        dbContext.QueueTickets.FirstOrDefaultAsync(ticket => ticket.Id == ticketId, cancellationToken);

    public async Task<IReadOnlyList<QueueTicket>> ListQueuedAsync(
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.QueueTickets
            .Where(ticket => ticket.Status == QueueTicketStatus.Queued)
            .OrderBy(ticket => ticket.EnqueuedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task AddAsync(QueueTicket ticket, CancellationToken cancellationToken) =>
        dbContext.QueueTickets.AddAsync(ticket, cancellationToken).AsTask();
}
