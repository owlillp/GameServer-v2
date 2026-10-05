namespace WalletService.Contracts.Dtos;

public sealed record CurrencyDto(
    string Code,
    string DisplayName,
    string Kind,
    bool IsActive,
    DateTimeOffset CreatedAt);
