using Shared.Core.Abstractions;
using WalletService.Contracts.Requests;

namespace WalletService.Core.Features.Wallets.Commands.TopUpWallet;

public sealed record TopUpWalletCommand(Guid WalletId, TopUpWalletRequest Request) : ICommand;
