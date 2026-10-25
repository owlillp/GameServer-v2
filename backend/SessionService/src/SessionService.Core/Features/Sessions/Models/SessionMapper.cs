using SessionService.Contracts.Dtos;
using SessionService.Core.Services;
using SessionService.Domain.Servers;
using SessionService.Domain.Sessions;

namespace SessionService.Core.Features.Sessions.Models;

public static class SessionMapper
{
    public static SessionDto ToDto(this Session session) =>
        new(
            session.Id,
            session.ServerId,
            session.Kind.ToWireName(),
            session.Mode,
            session.Region,
            session.Status.ToString().ToUpperInvariant(),
            session.PlayerCount,
            session.MaxPlayers,
            session.MatchId,
            session.CreatedAt,
            session.ClosedAt);

    public static GameServerDto ToDto(this GameServer server) =>
        new(
            server.Id,
            server.Address,
            server.Region,
            server.Modes,
            server.Capacity,
            server.Status.ToString().ToUpperInvariant(),
            server.RegisteredAt,
            server.LastHeartbeatAt);

    public static SessionTicketDto ToTicketDto(this Session session, GameServer server, JoinTicket ticket) =>
        new(
            session.Id,
            server.Address,
            session.Kind.ToWireName(),
            session.Mode,
            session.Region,
            ticket.Token,
            ticket.ExpiresAt);
}
