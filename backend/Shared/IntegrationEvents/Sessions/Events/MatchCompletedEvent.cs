namespace Shared.IntegrationEvents.Sessions.Events;

/// <summary>
/// One event per player. Published by SessionService (game server reports) and, during the
/// migration, by MatchmakingService. New fields are optional so existing consumers keep working.
/// </summary>
/// <param name="SessionId">Session the activity happened in; null for matches finished by an admin.</param>
/// <param name="ExternalRef">Reference of an outside owner of the match (e.g. a clan battle id).</param>
/// <param name="ReportPlayerCount">How many per-player events the same report produced: lets a consumer
/// know when it has the whole result.</param>
public sealed record MatchCompletedEvent(
    Guid MatchId,
    Guid AccountId,
    string Mode,
    bool IsWin,
    int Kills,
    int Score,
    DateTime CompletedAt,
    Guid? SessionId = null,
    string? ExternalRef = null,
    int? ReportPlayerCount = null);
