using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Services;
using MatchmakingService.Domain.Matches;
using Shared.Core.Database;
using Shared.IntegrationEvents.Sessions.Events;

namespace MatchmakingService.Core.Messaging.Handlers;

/// <summary>
/// The match session closed while the match is still active, i.e. the game server died:
/// with results the match is finished, without results it is cancelled and re-queued.
/// </summary>
public sealed class SessionClosedEventHandler(
    IMatchRepository matchRepository,
    MatchFinalizer finalizer,
    MatchRecovery recovery,
    ITransactionManager transactionManager,
    TimeProvider timeProvider)
{
    public async Task Handle(SessionClosedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.MatchId is null)
        {
            return;
        }

        (await transactionManager.BeginTransactionAsync(cancellationToken)).ThrowIfFailed();

        Match? match = await matchRepository.GetForUpdateAsync(@event.MatchId.Value, cancellationToken);
        if (match is null || match.Status != MatchStatus.Active || match.SessionId != @event.SessionId)
        {
            return;
        }

        MatchFinalization? finalization = null;

        if (match.HasResults)
        {
            var finishResult = await finalizer.FinishLockedAsync(match, cancellationToken);
            if (finishResult.IsFailure)
            {
                throw finishResult.Error.ToHandlerException();
            }

            finalization = finishResult.Value;
        }
        else
        {
            await recovery.CancelAndRequeueAsync(
                match,
                $"session-closed:{@event.Reason}",
                timeProvider.GetUtcNow().UtcDateTime,
                cancellationToken);
        }

        (await transactionManager.CommitTransactionAsync(cancellationToken)).ThrowIfFailed();

        if (finalization is not null)
        {
            await finalizer.CompleteAsync(finalization, cancellationToken);
        }
    }
}
