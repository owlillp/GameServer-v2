using Shared.Core.Abstractions;
using WalletService.Contracts.Requests;

namespace WalletService.Core.Features.Wallets.Commands.CreditWallet;

public sealed record CreditWalletCommand(Guid WalletId, CreditWalletRequest Request) : ICommand;
