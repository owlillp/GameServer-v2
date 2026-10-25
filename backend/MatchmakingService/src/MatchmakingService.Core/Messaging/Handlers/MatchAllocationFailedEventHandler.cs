using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Services;
using MatchmakingService.Domain.Matches;
using Shared.Core.Database;
using Shared.IntegrationEvents.Sessions.Events;

namespace MatchmakingService.Core.Messaging.Handlers;

public sealed class MatchAllocationFailedEventHandler(
    IMatchRepository matchRepository,
    MatchRecovery recovery,
    ITransactionManager transactionManager,
    TimeProvider timeProvider)
{
    public async Task Handle(MatchAllocationFailedEvent @event, CancellationToken cancellationToken)
    {
        (await transactionManager.BeginTransactionAsync(cancellationToken)).ThrowIfFailed();

        Match? match = await matchRepository.GetForUpdateAsync(@event.MatchId, cancellationToken);
        if (match is null || !match.IsWaitingForServer)
        {
            return;
        }

        await recovery.CancelAndRequeueAsync(
            match,
            $"allocation-failed:{@event.Reason}",
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        (await transactionManager.CommitTransactionAsync(cancellationToken)).ThrowIfFailed();
    }
}
