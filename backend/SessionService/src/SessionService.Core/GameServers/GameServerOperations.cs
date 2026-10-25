using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionService.Core.Abstractions;
using SessionService.Core.Configuration;
using SessionService.Core.Services;
using SessionService.Domain;
using SessionService.Domain.Reports;
using SessionService.Domain.Servers;
using SessionService.Domain.Sessions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace SessionService.Core.GameServers;

public sealed record ServerRegistration(Guid ServerId, int HeartbeatIntervalSeconds, string TicketPublicKeyPem, string TicketKeyId);

public sealed record TicketValidation(Guid AccountId, Session Session);

public sealed record ReportOutcome(bool Duplicate, bool Suspicious);

/// <summary>
/// Everything a game server can do. The gRPC layer only translates messages.
/// </summary>
public sealed class GameServerOperations(
    IGameServerRepository serverRepository,
    ISessionRepository sessionRepository,
    IRawReportRepository reportRepository,
    ISessionRegistry registry,
    ISessionEventPublisher eventPublisher,
    JoinTicketService ticketService,
    ITransactionManager transactionManager,
    IOptionsMonitor<SessionOptions> sessionOptions,
    IOptionsMonitor<AntiCheatOptions> antiCheatOptions,
    TimeProvider timeProvider,
    ILogger<GameServerOperations> logger)
{
    private static readonly JsonSerializerOptions PAYLOAD_JSON = new(JsonSerializerDefaults.Web);

    public async Task<Result<ServerRegistration, Error>> RegisterAsync(
        Guid? serverId,
        string address,
        string region,
        IReadOnlyList<string> modes,
        int capacity,
        CancellationToken cancellationToken)
    {
        DateTime now = Now();
        GameServer? server = serverId is null ? null : await serverRepository.GetAsync(serverId.Value, cancellationToken);

        if (server is null)
        {
            var registerResult = GameServer.Register(
                serverId ?? Guid.CreateVersion7(),
                address,
                region,
                modes,
                capacity,
                now);

            if (registerResult.IsFailure)
            {
                return registerResult.Error;
            }

            server = registerResult.Value;
            await serverRepository.AddAsync(server, cancellationToken);
        }
        else
        {
            var updateResult = server.Update(address, region, modes, capacity, now);
            if (updateResult.IsFailure)
            {
                return updateResult.Error;
            }
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        SessionOptions current = sessionOptions.CurrentValue;
        await registry.TouchServerAsync(server.Id, TimeSpan.FromSeconds(current.ServerTtlSeconds), cancellationToken);

        logger.LogInformation(
            "Game server {ServerId} registered at {Address} in {Region}",
            server.Id,
            server.Address,
            server.Region);

        return new ServerRegistration(
            server.Id,
            current.HeartbeatIntervalSeconds,
            ticketService.PublicKeyPem,
            ticketService.KeyId);
    }

    public async Task<UnitResult<Error>> HeartbeatAsync(
        Guid serverId,
        IReadOnlyCollection<Guid> sessionIds,
        CancellationToken cancellationToken)
    {
        SessionOptions current = sessionOptions.CurrentValue;

        await registry.TouchServerAsync(serverId, TimeSpan.FromSeconds(current.ServerTtlSeconds), cancellationToken);
        await registry.TouchSessionsAsync(sessionIds, TimeSpan.FromSeconds(current.SessionTtlSeconds), cancellationToken);

        GameServer? server = await serverRepository.GetAsync(serverId, cancellationToken);
        if (server is null)
        {
            return SessionErrors.ServerNotFound;
        }

        DateTime now = Now();
        if (server.Status == GameServerStatus.Online
            && now - server.LastHeartbeatAt < TimeSpan.FromSeconds(current.HeartbeatPersistSeconds))
        {
            return UnitResult.Success<Error>();
        }

        server.Heartbeat(now);
        return await transactionManager.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<TicketValidation, Error>> ValidateTicketAsync(
        Guid serverId,
        string ticket,
        CancellationToken cancellationToken)
    {
        var claimsResult = await ticketService.ValidateAsync(ticket);
        if (claimsResult.IsFailure)
        {
            return claimsResult.Error;
        }

        JoinTicketClaims claims = claimsResult.Value;
        if (claims.ServerId != serverId)
        {
            return SessionErrors.ServerNotOwner;
        }

        Session? session = await sessionRepository.GetAsync(claims.SessionId, cancellationToken);
        if (session is null)
        {
            return SessionErrors.SessionNotFound;
        }

        if (!session.IsOpen)
        {
            return SessionErrors.SessionClosed;
        }

        TimeSpan remaining = claims.ExpiresAt - Now();
        bool firstUse = await registry.TryUseTicketAsync(
            claims.TicketId,
            remaining > TimeSpan.Zero ? remaining + TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(5),
            cancellationToken);

        return firstUse
            ? new TicketValidation(claims.AccountId, session)
            : SessionErrors.TicketAlreadyUsed;
    }

    public async Task<UnitResult<Error>> PlayerJoinedAsync(
        Guid serverId,
        Guid sessionId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var sessionResult = await GetOwnedSessionAsync(serverId, sessionId, cancellationToken);
        if (sessionResult.IsFailure)
        {
            return sessionResult.Error;
        }

        Session session = sessionResult.Value;
        if (session.IsPresent(accountId))
        {
            return UnitResult.Success<Error>();
        }

        DateTime now = Now();
        var joinResult = session.Join(accountId, now);
        if (joinResult.IsFailure)
        {
            return joinResult.Error;
        }

        await eventPublisher.PublishPlayerJoinedAsync(session, accountId, now);
        return await transactionManager.SaveChangesAsync(cancellationToken);
    }

    public async Task<UnitResult<Error>> PlayerLeftAsync(
        Guid serverId,
        Guid sessionId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var sessionResult = await GetOwnedSessionAsync(serverId, sessionId, cancellationToken);
        if (sessionResult.IsFailure)
        {
            return sessionResult.Error;
        }

        Session session = sessionResult.Value;
        if (!session.IsPresent(accountId))
        {
            return session.HasPlayed(accountId)
                ? UnitResult.Success<Error>()
                : SessionErrors.PlayerNotInSession;
        }

        DateTime now = Now();
        session.Leave(accountId, now);

        await eventPublisher.PublishPlayerLeftAsync(session, accountId, now);
        return await transactionManager.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Stores the raw report and publishes <c>match-completed.session</c> in one transaction
    /// (outbox). A retry with the same activity id is recognised and published only once.
    /// </summary>
    public async Task<Result<ReportOutcome, Error>> ReportMatchAsync(
        Guid serverId,
        Guid sessionId,
        MatchReport report,
        CancellationToken cancellationToken)
    {
        RawReport? existing = await reportRepository.GetByActivityAsync(report.ActivityId, cancellationToken);
        if (existing is not null)
        {
            return existing.SessionId == sessionId
                ? new ReportOutcome(Duplicate: true, existing.IsSuspicious)
                : SessionErrors.ReportInvalid;
        }

        var sessionResult = await GetOwnedSessionAsync(serverId, sessionId, cancellationToken);
        if (sessionResult.IsFailure)
        {
            return sessionResult.Error;
        }

        Session session = sessionResult.Value;

        var validationResult = ReportChecks.Validate(report, session);
        if (validationResult.IsFailure)
        {
            return validationResult.Error;
        }

        IReadOnlyList<string> flags = ReportChecks.Flags(report, antiCheatOptions.CurrentValue.ToLimits());

        var rawReport = new RawReport(
            session.Id,
            serverId,
            report.ActivityId,
            session.MatchId ?? DeriveMatchId(report.ActivityId),
            string.IsNullOrWhiteSpace(report.Mode) ? session.Mode : report.Mode,
            JsonSerializer.Serialize(report, PAYLOAD_JSON),
            flags,
            Now());

        await reportRepository.AddAsync(rawReport, cancellationToken);
        await eventPublisher.PublishMatchCompletedAsync(session, rawReport, report);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            // A concurrent retry of the same report won the unique index on activity_id.
            return saveResult.Error.IsUniqueViolation()
                ? new ReportOutcome(Duplicate: true, rawReport.IsSuspicious)
                : saveResult.Error;
        }

        if (rawReport.IsSuspicious)
        {
            logger.LogWarning(
                "Suspicious report {ActivityId} from server {ServerId}: {Flags}",
                report.ActivityId,
                serverId,
                string.Join(", ", flags));
        }

        return new ReportOutcome(Duplicate: false, rawReport.IsSuspicious);
    }

    /// <summary>
    /// Open-world activities have no match id: use the activity id when it is a GUID,
    /// otherwise a stable GUID derived from it, so retries map to the same match.
    /// </summary>
    internal static Guid DeriveMatchId(string activityId)
    {
        if (Guid.TryParse(activityId, out Guid parsed))
        {
            return parsed;
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(activityId));
        return new Guid(hash.AsSpan(0, 16));
    }

    private async Task<Result<Session, Error>> GetOwnedSessionAsync(
        Guid serverId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        Session? session = await sessionRepository.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return SessionErrors.SessionNotFound;
        }

        return session.IsOwnedBy(serverId) ? session : SessionErrors.ServerNotOwner;
    }

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;
}
