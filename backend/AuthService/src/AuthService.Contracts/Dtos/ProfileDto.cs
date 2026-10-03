namespace AuthService.Contracts.Dtos;

public sealed record ProfileDto(
    Guid Id,
    string? Email,
    string? Username,
    string? DisplayName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ProfileBodyDto Profile);

public sealed record ProfileBodyDto(int? Age, string? Bio, string? Location);