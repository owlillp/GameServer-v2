using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain.Queues;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Queues.Queries.ListQueue;

public sealed class ListQueueHandler(IQueueRepository queueRepository)
    : IQueryHandler<IReadOnlyList<QueueTicketDto>, ListQueueQuery>
{
    public async Task<Result<IReadOnlyList<QueueTicketDto>, Error>> Handle(
        ListQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        int limit = Math.Clamp(query.Limit, 1, 200);

        IReadOnlyList<QueueTicket> tickets = await queueRepository.ListQueuedAsync(limit, cancellationToken);

        return tickets
            .Select(ticket => ticket.ToDto())
            .ToList();
    }
}
