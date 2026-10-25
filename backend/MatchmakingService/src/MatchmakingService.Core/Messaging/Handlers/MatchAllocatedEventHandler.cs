using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain.Matches;
using Microsoft.Extensions.Logging;
using Shared.Core.Database;
using Shared.IntegrationEvents.Sessions.Events;

namespace MatchmakingService.Core.Messaging.Handlers;

/// <summary>
/// Stores the game server of a match and tells the players (via Realtime) that they can connect.
/// A late answer for a match that was already cancelled is ignored.
/// </summary>
public sealed class MatchAllocatedEventHandler(
    IMatchRepository matchRepository,
    IMatchmakingEventPublisher eventPublisher,
    ITransactionManager transactionManager,
    TimeProvider timeProvider,
    ILogger<MatchAllocatedEventHandler> logger)
{
    public async Task Handle(MatchAllocatedEvent @event, CancellationToken cancellationToken)
    {
        (await transactionManager.BeginTransactionAsync(cancellationToken)).ThrowIfFailed();

        Match? match = await matchRepository.GetForUpdateAsync(@event.MatchId, cancellationToken);
        if (match is null || match.IsAllocated)
        {
            return;
        }

        var allocateResult = match.MarkAllocated(@event.SessionId, @event.ServerAddress, timeProvider.GetUtcNow().UtcDateTime);
        if (allocateResult.IsFailure)
        {
            logger.LogWarning(
                "Ignoring allocation of match {MatchId} to session {SessionId}: {Error}",
                @event.MatchId,
                @event.SessionId,
                allocateResult.Error.GetMessage());

            return;
        }

        await eventPublisher.PublishMatchReadyAsync(match, cancellationToken);

        (await transactionManager.CommitTransactionAsync(cancellationToken)).ThrowIfFailed();

        logger.LogInformation("Match {MatchId} is ready on {ServerAddress}", match.Id, @event.ServerAddress);
    }
}
