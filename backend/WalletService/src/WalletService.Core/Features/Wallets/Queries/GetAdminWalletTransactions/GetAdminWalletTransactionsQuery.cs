using Shared.Core.Abstractions;

namespace WalletService.Core.Features.Wallets.Queries.GetAdminWalletTransactions;

public sealed record GetAdminWalletTransactionsQuery(Guid WalletId, int Limit) : IQuery;
