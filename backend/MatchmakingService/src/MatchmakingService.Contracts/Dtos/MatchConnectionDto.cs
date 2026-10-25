namespace MatchmakingService.Contracts.Dtos;

/// <summary>
/// Where and with what ticket the player connects to the match's game server.
/// </summary>
public sealed record MatchConnectionDto(
    Guid SessionId,
    string ServerAddress,
    string Ticket,
    DateTime ExpiresAt);
