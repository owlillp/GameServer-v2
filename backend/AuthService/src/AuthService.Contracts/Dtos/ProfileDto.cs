namespace AuthService.Contracts.Dtos;

public sealed record ProfileDto(
    Guid Id,
    string? Email,
    string? Username,
    DateTime CreatedAt,
    DateTime UpdatedAt);
