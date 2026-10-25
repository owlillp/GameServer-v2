namespace SessionService.Contracts.Dtos;

public sealed record GameServerDto(
    Guid Id,
    string Address,
    string Region,
    IReadOnlyList<string> Modes,
    int Capacity,
    string Status,
    DateTime RegisteredAt,
    DateTime LastHeartbeatAt);
