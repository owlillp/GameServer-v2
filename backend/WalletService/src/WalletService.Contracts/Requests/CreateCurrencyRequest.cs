namespace WalletService.Contracts.Requests;

public sealed record CreateCurrencyRequest(
    string Code,
    string DisplayName,
    string Kind);
