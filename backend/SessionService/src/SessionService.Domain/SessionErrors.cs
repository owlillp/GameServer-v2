using Shared.SharedKernel.Errors;

namespace SessionService.Domain;

public static class SessionErrors
{
    public static Error ServerInvalid { get; } =
        Error.Validation("session.server.invalid", "Server address, region, modes and capacity are required", "server");

    public static Error ServerNotFound { get; } =
        Error.NotFound("session.server.not_found", "Game server is not registered", "serverId");

    public static Error ServerNotOwner { get; } =
        Error.Authorization("session.server.not_owner", "The session belongs to another game server", "serverId");

    public static Error NoServerAvailable { get; } =
        Error.Conflict("session.server.unavailable", "No game server is available in this region for this mode", "region");

    public static Error RegionInvalid { get; } =
        Error.Validation("session.region.invalid", "Region is invalid", "region");

    public static Error ModeInvalid { get; } =
        Error.Validation("session.mode.invalid", "Mode is invalid", "mode");

    public static Error SessionNotFound { get; } =
        Error.NotFound("session.not_found", "Session was not found", "sessionId");

    public static Error SessionClosed { get; } =
        Error.Conflict("session.closed", "Session is closed", "sessionId");

    public static Error SessionFull { get; } =
        Error.Conflict("session.full", "Session is full", "sessionId");

    public static Error SessionNotJoinable { get; } =
        Error.Conflict("session.not_joinable", "Only open-world sessions can be joined directly", "sessionId");

    public static Error PlayerNotInSession { get; } =
        Error.Validation("session.player.not_in_session", "Player is not in this session", "accountId");

    public static Error FriendNotInSession { get; } =
        Error.NotFound("session.friend.not_in_session", "The player is not in an open-world session", "accountId");

    public static Error TicketInvalid { get; } =
        Error.Authentication("session.ticket.invalid", "Join ticket is invalid or expired", "ticket");

    public static Error TicketAlreadyUsed { get; } =
        Error.Conflict("session.ticket.used", "Join ticket has already been used", "ticket");

    public static Error MatchSessionNotReady { get; } =
        Error.NotFound("session.match.not_ready", "No open session is allocated for this match", "matchId");

    public static Error ReportInvalid { get; } =
        Error.Validation("session.report.invalid", "Match report is invalid", "report");

    public static Error ReportPlayerUnknown { get; } =
        Error.Validation("session.report.player_unknown", "A reported player was never in the session", "players");
}
