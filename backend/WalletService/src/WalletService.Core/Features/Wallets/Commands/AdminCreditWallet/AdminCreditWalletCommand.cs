using Shared.Core.Abstractions;
using WalletService.Contracts.Requests;

namespace WalletService.Core.Features.Wallets.Commands.AdminCreditWallet;

public sealed record AdminCreditWalletCommand(Guid WalletId, AdminCreditWalletRequest Request) : ICommand;
