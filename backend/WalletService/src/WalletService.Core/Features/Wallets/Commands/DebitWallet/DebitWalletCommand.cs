using Shared.Core.Abstractions;
using WalletService.Contracts.Requests;

namespace WalletService.Core.Features.Wallets.Commands.DebitWallet;

public sealed record DebitWalletCommand(Guid WalletId, DebitWalletRequest Request) : ICommand;
