using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using SessionService.Contracts.Dtos;
using SessionService.Core.Abstractions;
using SessionService.Core.Configuration;
using SessionService.Core.Features.Sessions.Models;
using SessionService.Domain;
using SessionService.Domain.Servers;
using SessionService.Domain.Sessions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace SessionService.Core.Services;

/// <summary>
/// Chooses where a player enters the open world: an existing session with free slots, or a new
/// session on a newly allocated server. The capacity check here is advisory; the hard limit is
/// enforced when the game server reports the player as joined.
/// </summary>
public sealed class SessionPlacement(
    ISessionRepository sessionRepository,
    IGameServerRepository serverRepository,
    IGameServerAllocator allocator,
    ISessionRegistry registry,
    JoinTicketService ticketService,
    ITransactionManager transactionManager,
    IOptionsMonitor<SessionOptions> options,
    TimeProvider timeProvider)
{
    public async Task<Result<SessionTicketDto, Error>> JoinFreeRoamAsync(
        Guid accountId,
        string region,
        string mode,
        CancellationToken cancellationToken)
    {
        SessionOptions current = options.CurrentValue;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        Session? session = await FindJoinableAsync(region, mode, current.JoinCandidates, cancellationToken);
        if (session is not null)
        {
            return await IssueAsync(accountId, session, now, cancellationToken);
        }

        int maxPlayers = current.FreeRoamMaxPlayers;
        GameServer? server = await allocator.AllocateAsync(region, mode, maxPlayers, cancellationToken);
        if (server is null)
        {
            return SessionErrors.NoServerAvailable;
        }

        var createResult = Session.Create(server, SessionKind.FreeRoam, mode, maxPlayers, matchId: null, now);
        if (createResult.IsFailure)
        {
            return createResult.Error;
        }

        session = createResult.Value;
        await sessionRepository.AddAsync(session, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        await registry.TouchSessionsAsync(
            [session.Id],
            TimeSpan.FromSeconds(current.NewSessionTtlSeconds),
            cancellationToken);

        return IssueTicket(accountId, session, server, now);
    }

    public async Task<Result<SessionTicketDto, Error>> IssueAsync(
        Guid accountId,
        Session session,
        DateTime now,
        CancellationToken cancellationToken)
    {
        GameServer? server = await serverRepository.GetAsync(session.ServerId, cancellationToken);
        if (server is null)
        {
            return SessionErrors.ServerNotFound;
        }

        return IssueTicket(accountId, session, server, now);
    }

    /// <summary>
    /// The fullest live session with a free slot: players are packed before new sessions open.
    /// </summary>
    private async Task<Session?> FindJoinableAsync(
        string region,
        string mode,
        int candidates,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Session> sessions = await sessionRepository.ListJoinableFreeRoamAsync(
            region,
            mode,
            candidates,
            cancellationToken);

        if (sessions.Count == 0)
        {
            return null;
        }

        IReadOnlySet<Guid> alive = await registry.GetAliveAsync(
            [.. sessions.Select(session => session.Id)],
            cancellationToken);

        return sessions.FirstOrDefault(session => alive.Contains(session.Id));
    }

    private SessionTicketDto IssueTicket(Guid accountId, Session session, GameServer server, DateTime now)
    {
        SessionMetrics.Joins.Add(1, new KeyValuePair<string, object?>("kind", session.Kind.ToWireName()));
        return session.ToTicketDto(server, ticketService.Issue(accountId, session.Id, server.Id, now));
    }
}
