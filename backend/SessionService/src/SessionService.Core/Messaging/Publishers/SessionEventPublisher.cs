using SessionService.Core.Abstractions;
using SessionService.Domain.Reports;
using SessionService.Domain.Sessions;
using Shared.IntegrationEvents.Sessions.Events;

namespace SessionService.Core.Messaging.Publishers;

public sealed class SessionEventPublisher(IOutboxService outbox) : ISessionEventPublisher
{
    public Task PublishPlayerJoinedAsync(Session session, Guid accountId, DateTime joinedAt) =>
        outbox.PublishAsync(new PlayerJoinedSessionEvent(
            session.Id,
            session.ServerId,
            accountId,
            session.Kind.ToWireName(),
            session.Mode,
            session.Region,
            joinedAt));

    public Task PublishPlayerLeftAsync(Session session, Guid accountId, DateTime leftAt) =>
        outbox.PublishAsync(new PlayerLeftSessionEvent(session.Id, session.ServerId, accountId, leftAt));

    public Task PublishSessionClosedAsync(Session session) =>
        outbox.PublishAsync(new SessionClosedEvent(
            session.Id,
            session.ServerId,
            session.Kind.ToWireName(),
            session.MatchId,
            session.CloseReason ?? string.Empty,
            session.ClosedAt ?? DateTime.UtcNow));

    /// <summary>
    /// The existing per-player contract: Matchmaking, EventService and others already consume it.
    /// </summary>
    public async Task PublishMatchCompletedAsync(Session session, RawReport report, MatchReport data)
    {
        foreach (ReportedPlayer player in data.Players)
        {
            await outbox.PublishAsync(new MatchCompletedEvent(
                report.MatchId,
                player.AccountId,
                report.Mode,
                player.IsWinner,
                player.Kills,
                player.Score,
                data.EndedAt,
                session.Id,
                ExternalRef: null));
        }
    }
}
