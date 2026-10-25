using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatchmakingService.Core.Services;

/// <summary>
/// A match that never started (no game server, or the server died before reporting) is
/// cancelled and its parties go back to the queue ahead of everyone else, keeping their
/// queue time. After <see cref="MatchmakingOptions.MaxAllocationAttempts"/> they are released.
/// Must run inside a transaction with the match row locked; the caller saves.
/// </summary>
public sealed class MatchRecovery(
    IMatchmakingEventPublisher eventPublisher,
    IPartyRepository partyRepository,
    IQueueRepository queueRepository,
    IOptionsMonitor<MatchmakingOptions> options,
    ILogger<MatchRecovery> logger)
{
    public async Task CancelAndRequeueAsync(Match match, string reason, DateTime now, CancellationToken cancellationToken)
    {
        if (match.Cancel(now).IsFailure)
        {
            return;
        }

        // External matches have no queue to go back to: the owner (clan wars) decides what to do.
        if (match.IsExternal)
        {
            await eventPublisher.PublishExternalCancelledAsync(match, reason, cancellationToken);
            logger.LogWarning("External match {MatchId} ({ExternalRef}) cancelled: {Reason}", match.Id, match.ExternalRef, reason);
            return;
        }

        IReadOnlyList<QueueTicket> previousTickets = await queueRepository.ListByMatchAsync(match.Id, cancellationToken);
        Guid[] partyIds = [.. match.Parties.Select(party => party.PartyId)];
        IReadOnlyList<Party> parties = await partyRepository.ListAsync(partyIds, cancellationToken);
        int maxAttempts = options.CurrentValue.MaxAllocationAttempts;

        foreach (Party party in parties)
        {
            QueueTicket? previous = previousTickets.FirstOrDefault(ticket => ticket.PartyId == party.Id);

            if (previous is null || previous.Attempt >= maxAttempts || party.ReturnToQueue(now).IsFailure)
            {
                party.ReleaseFromMatch(now);
                continue;
            }

            var ticketResult = previous.Requeue(party, now);
            if (ticketResult.IsFailure)
            {
                party.ReleaseFromQueue(now);
                continue;
            }

            await queueRepository.AddAsync(ticketResult.Value, cancellationToken);
        }

        logger.LogWarning("Match {MatchId} cancelled before start ({Reason}), parties re-queued", match.Id, reason);
    }
}
