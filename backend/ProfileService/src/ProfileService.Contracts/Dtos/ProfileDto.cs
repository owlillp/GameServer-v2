namespace ProfileService.Contracts.Dtos;

public sealed record ProfileDto(
    Guid Id,
    string Nickname,
    string? Region,
    DateTime CreatedAt);
