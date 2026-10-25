using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionService.Core.Abstractions;
using SessionService.Core.Configuration;
using SessionService.Domain;
using SessionService.Domain.Servers;
using SessionService.Domain.Sessions;
using Shared.Core.Database;
using Shared.IntegrationEvents.Matchmaking.Commands;

namespace SessionService.Core.Messaging.Handlers;

/// <summary>
/// Creates a match session on a game server and answers with <c>match-allocated.session</c>,
/// or <c>match-allocation-failed.session</c> when no server is available.
/// A repeated command for the same match re-sends the answer instead of allocating twice.
/// </summary>
public sealed class AllocateMatchCommandHandler(
    ISessionRepository sessionRepository,
    IGameServerRepository serverRepository,
    IGameServerAllocator allocator,
    ISessionRegistry registry,
    ISessionEventPublisher publisher,
    ITransactionManager transactionManager,
    IOptionsMonitor<SessionOptions> options,
    TimeProvider timeProvider,
    ILogger<AllocateMatchCommandHandler> logger)
{
    public const string REASON_NO_SERVER = "no-server";
    public const string REASON_INVALID = "invalid-request";

    public async Task Handle(AllocateMatchCommand command, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        Session? existing = await sessionRepository.GetByMatchAsync(command.MatchId, cancellationToken);
        if (existing is not null)
        {
            await RepeatAnswerAsync(existing, now, cancellationToken);
            return;
        }

        string region = SessionNames.NormalizeRegion(command.Region);
        string mode = SessionNames.NormalizeMode(command.Mode);
        int players = command.Participants.Count;

        if (region.Length == 0 || mode.Length == 0 || players == 0)
        {
            await FailAsync(command.MatchId, REASON_INVALID, now, cancellationToken);
            return;
        }

        GameServer? server = await allocator.AllocateAsync(region, mode, players, cancellationToken);
        if (server is null)
        {
            logger.LogWarning("No game server for match {MatchId} in {Region} / {Mode}", command.MatchId, region, mode);
            await FailAsync(command.MatchId, REASON_NO_SERVER, now, cancellationToken);
            return;
        }

        var createResult = Session.Create(server, SessionKind.Match, mode, players, command.MatchId, now);
        if (createResult.IsFailure)
        {
            await FailAsync(command.MatchId, REASON_INVALID, now, cancellationToken);
            return;
        }

        Session session = createResult.Value;
        await sessionRepository.AddAsync(session, cancellationToken);
        await publisher.PublishMatchAllocatedAsync(session, server, now);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure && saveResult.Error.IsUniqueViolation())
        {
            // A concurrent delivery of the same command created the session and answered.
            return;
        }

        saveResult.ThrowIfFailed();

        await registry.TouchSessionsAsync(
            [session.Id],
            TimeSpan.FromSeconds(options.CurrentValue.NewSessionTtlSeconds),
            cancellationToken);

        logger.LogInformation(
            "Match {MatchId} allocated to server {ServerId} as session {SessionId}",
            command.MatchId,
            server.Id,
            session.Id);
    }

    private async Task RepeatAnswerAsync(Session session, DateTime now, CancellationToken cancellationToken)
    {
        GameServer? server = await serverRepository.GetAsync(session.ServerId, cancellationToken);

        if (session.IsOpen && server is not null)
        {
            await publisher.PublishMatchAllocatedAsync(session, server, now);
        }
        else
        {
            await publisher.PublishMatchAllocationFailedAsync(session.MatchId!.Value, REASON_NO_SERVER, now);
        }

        (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();
    }

    private async Task FailAsync(Guid matchId, string reason, DateTime now, CancellationToken cancellationToken)
    {
        await publisher.PublishMatchAllocationFailedAsync(matchId, reason, now);
        (await transactionManager.SaveChangesAsync(cancellationToken)).ThrowIfFailed();
    }
}
