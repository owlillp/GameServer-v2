using Shared.Core.Abstractions;

namespace WalletService.Core.Features.Currencies.Commands.DeleteCurrency;

public sealed record DeleteCurrencyCommand(string Code) : ICommand;
