using Microsoft.AspNetCore.Mvc;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;
using WalletService.Contracts.Dtos;
using WalletService.Contracts.Requests;
using WalletService.Core.Features.Wallets.Commands.SpentWallet;
using WalletService.Core.Features.Wallets.Commands.TopUpWallet;
using WalletService.Core.Features.Wallets.Queries.GetMyWallet;
using WalletService.Core.Features.Wallets.Queries.GetTransactions;

namespace WalletService.Web.Endpoints.Wallets;

public sealed class WalletEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/wallets";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Wallets")
            .RequireAuthorization();

        group.MapGet("/me", GetMyWallet)
            .WithName("Wallets.Me.Get")
            .WithSummary("Балансы текущего игрока");

        group.MapGet("/me/transactions", GetTransactions)
            .WithName("Wallets.Me.Transactions")
            .WithSummary("История операций текущего игрока");

        group.MapPost("/{walletId:guid}/top-up", TopUpWallet)
            .WithName("Wallets.TopUp")
            .WithSummary("Начисление на кошелёк (внутренний вызов Store/наград)");

        group.MapPost("/{walletId:guid}/spent", SpentWallet)
            .WithName("Wallets.Spent")
            .WithSummary("Списание с кошелька (внутренний вызов Store)");

        return group;
    }

    private static async Task<EndpointResult<WalletDto>> GetMyWallet(
        [FromServices] IQueryHandler<WalletDto, GetMyWalletQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetMyWalletQuery(), ct);

    private static async Task<EndpointResult<IReadOnlyList<LedgerEntryDto>>> GetTransactions(
        [FromQuery] int limit,
        [FromServices] IQueryHandler<IReadOnlyList<LedgerEntryDto>, GetWalletTransactionsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetWalletTransactionsQuery(limit), ct);

    private static async Task<EndpointResult<WalletDto>> TopUpWallet(
        [FromRoute] Guid walletId,
        [FromBody] TopUpWalletRequest request,
        [FromServices] ICommandHandler<WalletDto, TopUpWalletCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new TopUpWalletCommand(walletId, request), ct);

    private static async Task<EndpointResult<WalletDto>> SpentWallet(
        [FromRoute] Guid walletId,
        [FromBody] SpentWalletRequest request,
        [FromServices] ICommandHandler<WalletDto, SpentWalletCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new SpentWalletCommand(walletId, request), ct);
}
