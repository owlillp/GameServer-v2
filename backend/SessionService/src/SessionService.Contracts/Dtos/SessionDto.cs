namespace SessionService.Contracts.Dtos;

public sealed record SessionDto(
    Guid Id,
    Guid ServerId,
    string Kind,
    string Mode,
    string Region,
    string Status,
    int PlayerCount,
    int MaxPlayers,
    Guid? MatchId,
    DateTime CreatedAt,
    DateTime? ClosedAt);
