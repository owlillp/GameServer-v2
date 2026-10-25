namespace Shared.IntegrationEvents.ClanWars.Events;

public sealed record BattleFoundEvent(
    Guid BattleId,
    Guid SeasonId,
    Guid ClanAId,
    Guid ClanBId,
    IReadOnlyList<Guid> SquadA,
    IReadOnlyList<Guid> SquadB,
    DateTime FoundAt);

/// <param name="Status">finished | cancelled.</param>
/// <param name="WinnerClanId">Null for a draw or a cancelled battle.</param>
public sealed record BattleFinishedEvent(
    Guid BattleId,
    Guid SeasonId,
    Guid ClanAId,
    Guid ClanBId,
    string Status,
    Guid? WinnerClanId,
    long PointsA,
    long PointsB,
    DateTime FinishedAt);
