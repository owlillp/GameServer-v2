using Shared.Core.Abstractions;
using WalletService.Contracts.Requests;

namespace WalletService.Core.Features.Wallets.Commands.AdminDebitWallet;

public sealed record AdminDebitWalletCommand(Guid WalletId, AdminDebitWalletRequest Request) : ICommand;
