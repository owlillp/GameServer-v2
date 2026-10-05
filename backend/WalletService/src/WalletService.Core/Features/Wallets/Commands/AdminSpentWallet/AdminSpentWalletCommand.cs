using Shared.Core.Abstractions;
using WalletService.Contracts.Requests;

namespace WalletService.Core.Features.Wallets.Commands.AdminSpentWallet;

public sealed record AdminSpentWalletCommand(Guid WalletId, AdminSpentWalletRequest Request) : ICommand;
