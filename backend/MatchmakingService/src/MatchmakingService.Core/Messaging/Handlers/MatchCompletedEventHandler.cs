using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Core.Services;
using MatchmakingService.Domain.Matches;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Database;
using Shared.IntegrationEvents.Sessions.Events;

namespace MatchmakingService.Core.Messaging.Handlers;

/// <summary>
/// Results reported by the game server (one event per player). Each event is recorded on the
/// match; when the whole report has arrived the match is finished. Replaces the admin
/// SubmitResults when <see cref="ResultsSource.Session"/> is on.
/// </summary>
public sealed class MatchCompletedEventHandler(
    IMatchRepository matchRepository,
    MatchFinalizer finalizer,
    ITransactionManager transactionManager,
    IOptionsMonitor<MatchmakingOptions> options,
    TimeProvider timeProvider,
    ILogger<MatchCompletedEventHandler> logger)
{
    public async Task Handle(MatchCompletedEvent @event, CancellationToken cancellationToken)
    {
        // Events without a session are the ones matchmaking itself publishes in admin mode.
        if (options.CurrentValue.ResultsSource != ResultsSource.Session || @event.SessionId is null)
        {
            return;
        }

        (await transactionManager.BeginTransactionAsync(cancellationToken)).ThrowIfFailed();

        // Open-world activities have no match in matchmaking.
        Match? match = await matchRepository.GetForUpdateAsync(@event.MatchId, cancellationToken);
        if (match is null || match.Status != MatchStatus.Active)
        {
            return;
        }

        var submitResult = match.SubmitResults(
            [new MatchResultData(@event.AccountId, Math.Max(0, @event.Score), Math.Max(0, @event.Kills))],
            timeProvider.GetUtcNow().UtcDateTime);

        if (submitResult.IsFailure)
        {
            logger.LogWarning(
                "Rejected result of {AccountId} for match {MatchId}: {Error}",
                @event.AccountId,
                @event.MatchId,
                submitResult.Error.GetMessage());

            return;
        }

        int expected = @event.ReportPlayerCount ?? match.Participants.Count;
        MatchFinalization? finalization = null;

        if (match.Results.Count >= expected)
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
            (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();
        }

        (await transactionManager.CommitTransactionAsync(cancellationToken)).ThrowIfFailed();

        if (finalization is not null)
        {
            await finalizer.CompleteAsync(finalization, cancellationToken);
        }
    }
}
