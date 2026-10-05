using Shared.Core.Abstractions;
using WalletService.Contracts.Requests;

namespace WalletService.Core.Features.Wallets.Commands.AdminTopUpWallet;

public sealed record AdminTopUpWalletCommand(Guid WalletId, AdminTopUpWalletRequest Request) : ICommand;
