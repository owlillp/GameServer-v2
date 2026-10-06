using Shared.Core.Abstractions;
using WalletService.Contracts.Requests;

namespace WalletService.Core.Features.Currencies.Commands.CreateCurrency;

public sealed record CreateCurrencyCommand(CreateCurrencyRequest Request) : ICommand;
