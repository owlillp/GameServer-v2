using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Domain.Matches;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Database;
using Shared.IntegrationEvents.Matchmaking.Commands;

namespace MatchmakingService.Core.Messaging.Handlers;

/// <summary>
/// Creates a match for teams chosen elsewhere (clan battles) and, with game servers on,
/// asks SessionService for a server in the same transaction. A repeated command for the
/// same external reference is a no-op (lookup + unique index).
/// </summary>
public sealed class CreateExternalMatchCommandHandler(
    IMatchRepository matchRepository,
    IMatchmakingEventPublisher eventPublisher,
    ITransactionManager transactionManager,
    IOptionsMonitor<MatchmakingOptions> options,
    TimeProvider timeProvider,
    ILogger<CreateExternalMatchCommandHandler> logger)
{
    public async Task Handle(CreateExternalMatchCommand command, CancellationToken cancellationToken)
    {
        if (await matchRepository.GetByExternalRefAsync(command.ExternalRef, cancellationToken) is not null)
        {
            return;
        }

        MatchmakingOptions current = options.CurrentValue;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        var matchResult = Match.CreateExternal(
            command.ExternalRef,
            command.Mode,
            command.Region,
            command.Teams.ToDictionary(team => team.Side, team => team.AccountIds),
            now,
            now.AddMinutes(current.ExternalMatchDurationMinutes));

        if (matchResult.IsFailure)
        {
            logger.LogWarning("Rejected external match {ExternalRef}: {Error}", command.ExternalRef, matchResult.Error.GetMessage());
            return;
        }

        Match match = matchResult.Value;
        await matchRepository.AddAsync(match, cancellationToken);

        if (current.ResultsSource == ResultsSource.Session)
        {
            match.RequestAllocation(now);
            await eventPublisher.PublishAllocateMatchAsync(match, cancellationToken);
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure && saveResult.Error.IsUniqueViolation())
        {
            return;
        }

        saveResult.ThrowIfFailed();
        MatchmakingMetrics.MatchesCreated.Add(1);

        logger.LogInformation("External match {MatchId} created for {ExternalRef}", match.Id, command.ExternalRef);
    }
}
