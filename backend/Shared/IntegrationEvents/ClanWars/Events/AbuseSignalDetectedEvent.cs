namespace Shared.IntegrationEvents.ClanWars.Events;

/// <summary>
/// Suspicious clan wars activity for the future Moderation service (no consumer yet).
/// </summary>
/// <param name="Kind">weak-opponent | repeated-pair | no-contest.</param>
public sealed record AbuseSignalDetectedEvent(
    Guid SignalId,
    string Kind,
    Guid SeasonId,
    IReadOnlyList<Guid> ClanIds,
    Guid? BattleId,
    string Details,
    DateTime DetectedAt);
