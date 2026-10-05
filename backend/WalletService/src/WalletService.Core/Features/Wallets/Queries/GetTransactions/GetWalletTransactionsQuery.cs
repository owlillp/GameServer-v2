using Shared.Core.Abstractions;

namespace WalletService.Core.Features.Wallets.Queries.GetTransactions;

public sealed record GetWalletTransactionsQuery(int Limit) : IQuery;
