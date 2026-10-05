using Shared.Core.Abstractions;

namespace WalletService.Core.Features.Currencies.Commands.CreateCurrency;

public sealed record CreateCurrencyCommand(
    string Code,
    string DisplayName,
    string Kind) : ICommand;
