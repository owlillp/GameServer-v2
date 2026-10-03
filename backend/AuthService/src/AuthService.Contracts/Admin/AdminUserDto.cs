namespace AuthService.Contracts.Admin;

public sealed record AdminUserDto(
    Guid Id,
    string? Email,
    string? UserName,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    bool IsLockedOut,
    DateTime CreatedAt);