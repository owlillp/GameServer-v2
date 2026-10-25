namespace Shared.IntegrationEvents.Matchmaking.Commands;

/// <summary>
/// Matchmaking asks SessionService for a game server for a match. Idempotent by <see cref="MatchId"/>.
/// </summary>
public sealed record AllocateMatchCommand(
    Guid MatchId,
    string Mode,
    string Region,
    IReadOnlyList<AllocateMatchParticipant> Participants,
    DateTime RequestedAt);

public sealed record AllocateMatchParticipant(Guid AccountId, Guid PartyId, int Team);
