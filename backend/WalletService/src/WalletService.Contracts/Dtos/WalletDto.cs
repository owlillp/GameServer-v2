namespace WalletService.Contracts.Dtos;

public sealed record WalletDto(
    Guid WalletId,
    IReadOnlyList<BalanceDto> Balances,
    DateTime CreatedAt,
    DateTime UpdatedAt);
