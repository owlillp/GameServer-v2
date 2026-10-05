using Shared.Core.Abstractions;
using WalletService.Contracts.Requests;

namespace WalletService.Core.Features.Wallets.Commands.SpentWallet;

public sealed record SpentWalletCommand(Guid WalletId, SpentWalletRequest Request) : ICommand;
