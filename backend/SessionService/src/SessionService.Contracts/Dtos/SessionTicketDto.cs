namespace SessionService.Contracts.Dtos;

/// <summary>
/// Where to connect and the short-lived join ticket the game server will check.
/// </summary>
public sealed record SessionTicketDto(
    Guid SessionId,
    string ServerAddress,
    string Kind,
    string Mode,
    string Region,
    string Ticket,
    DateTime ExpiresAt);
