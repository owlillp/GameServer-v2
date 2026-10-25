namespace Shared.IntegrationEvents.ClanWars.Events;

/// <summary>
/// ClanWars grants clan points (season rewards). Applied once per <see cref="OperationId"/>.
/// </summary>
public sealed record ClanPointsGrantedEvent(
    Guid OperationId,
    Guid ClanId,
    long Amount,
    string Reason,
    DateTime GrantedAt);
