namespace AuthService.Contracts.Auth;

public sealed record ServiceInfoDto(
    string Service,
    string Version,
    string Status);
