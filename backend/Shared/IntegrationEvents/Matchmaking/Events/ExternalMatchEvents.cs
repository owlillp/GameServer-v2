namespace Shared.IntegrationEvents.Matchmaking.Events;

public sealed record ExternalMatchStartedEvent(
    string ExternalRef,
    Guid MatchId,
    string ServerAddress,
    DateTime StartedAt);

/// <param name="WinnerSide">Null for a draw.</param>
public sealed record ExternalMatchCompletedEvent(
    string ExternalRef,
    Guid MatchId,
    string Mode,
    int? WinnerSide,
    IReadOnlyList<ExternalMatchTeamResult> Teams,
    DateTime CompletedAt);

public sealed record ExternalMatchTeamResult(int Side, long Score, IReadOnlyList<ExternalMatchPlayerResult> Players);

public sealed record ExternalMatchPlayerResult(Guid AccountId, long Score, int Kills);

/// <param name="Reason">no-server | allocation-timeout | session-closed | expired | admin.</param>
public sealed record ExternalMatchCancelledEvent(
    string ExternalRef,
    Guid MatchId,
    string Reason,
    DateTime CancelledAt);
