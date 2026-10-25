namespace Shared.IntegrationEvents.Matchmaking.Commands;

/// <summary>
/// A match whose teams are chosen by another service (e.g. a clan battle) instead of the queue.
/// Matchmaking plays it on a game server like any match. Idempotent by <see cref="ExternalRef"/>.
/// </summary>
public sealed record CreateExternalMatchCommand(
    string ExternalRef,
    string Mode,
    string Region,
    IReadOnlyList<ExternalMatchTeam> Teams,
    DateTime RequestedAt);

/// <param name="Side">1 or 2.</param>
public sealed record ExternalMatchTeam(int Side, IReadOnlyList<Guid> AccountIds);
