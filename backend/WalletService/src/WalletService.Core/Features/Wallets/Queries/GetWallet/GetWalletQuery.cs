using Shared.Core.Abstractions;

namespace WalletService.Core.Features.Wallets.Queries.GetWallet;

public sealed record GetWalletQuery(Guid WalletId) : IQuery;
