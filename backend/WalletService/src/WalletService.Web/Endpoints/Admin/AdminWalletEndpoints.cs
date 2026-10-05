using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authorization;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;
using WalletService.Contracts.Dtos;
using WalletService.Contracts.Requests;
using WalletService.Core.Features.Currencies.Commands.CreateCurrency;
using WalletService.Core.Features.Currencies.Commands.DeleteCurrency;
using WalletService.Core.Features.Wallets.Commands.AdminDebitWallet;
using WalletService.Core.Features.Wallets.Commands.AdminCreditWallet;
using WalletService.Core.Features.Wallets.Queries.GetAdminWalletTransactions;
using WalletService.Core.Features.Wallets.Queries.GetWallet;
using WalletService.Domain.Authorization;

namespace WalletService.Web.Endpoints.Admin;

public sealed class AdminWalletEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/admin/wallets";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Admin.Wallets")
            .RequireAuthorization();

        group.MapPost("/{walletId:guid}/credit", AdminCreditWallet)
            .WithName("Admin.Wallets.Credit")
            .WithSummary("Начисление игроку админом/модератором")
            .RequirePermissions(WalletPermissions.Wallets.MANAGE);

        group.MapPost("/{walletId:guid}/debit", AdminDebitWallet)
            .WithName("Admin.Wallets.Debit")
            .WithSummary("Списание у игрока админом/модератором")
            .RequirePermissions(WalletPermissions.Wallets.MANAGE);

        group.MapPost("/currencies", CreateCurrency)
            .WithName("Admin.Wallets.Currencies.Create")
            .WithSummary("Создать валюту")
            .RequirePermissions(WalletPermissions.Wallets.MANAGE);

        group.MapDelete("/currencies/{code}", DeleteCurrency)
            .WithName("Admin.Wallets.Currencies.Delete")
            .WithSummary("Удалить (деактивировать) валюту")
            .RequirePermissions(WalletPermissions.Wallets.MANAGE);

        group.MapGet("/{walletId:guid}", GetWallet)
            .WithName("Admin.Wallets.Get")
            .WithSummary("Карточка кошелька игрока")
            .RequirePermissions(WalletPermissions.Wallets.VIEW);

        group.MapGet("/{walletId:guid}/transactions", GetWalletTransactions)
            .WithName("Admin.Wallets.Transactions")
            .WithSummary("История операций игрока")
            .RequirePermissions(WalletPermissions.Wallets.VIEW);

        return group;
    }

    private static async Task<EndpointResult<WalletDto>> AdminCreditWallet(
        [FromRoute] Guid walletId,
        [FromBody] AdminCreditWalletRequest request,
        [FromServices] ICommandHandler<WalletDto, AdminCreditWalletCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new AdminCreditWalletCommand(walletId, request), ct);

    private static async Task<EndpointResult<WalletDto>> AdminDebitWallet(
        [FromRoute] Guid walletId,
        [FromBody] AdminDebitWalletRequest request,
        [FromServices] ICommandHandler<WalletDto, AdminDebitWalletCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new AdminDebitWalletCommand(walletId, request), ct);

    private static async Task<EndpointResult<CurrencyDto>> CreateCurrency(
        [FromBody] CreateCurrencyRequest request,
        [FromServices] ICommandHandler<CurrencyDto, CreateCurrencyCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new CreateCurrencyCommand(request), ct);

    private static async Task<EndpointResult<CurrencyDto>> DeleteCurrency(
        [FromRoute] string code,
        [FromServices] ICommandHandler<CurrencyDto, DeleteCurrencyCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new DeleteCurrencyCommand(code), ct);

    private static async Task<EndpointResult<WalletDto>> GetWallet(
        [FromRoute] Guid walletId,
        [FromServices] IQueryHandler<WalletDto, GetWalletQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetWalletQuery(walletId), ct);

    private static async Task<EndpointResult<IReadOnlyList<LedgerEntryDto>>> GetWalletTransactions(
        [FromRoute] Guid walletId,
        [FromQuery] int limit,
        [FromServices] IQueryHandler<IReadOnlyList<LedgerEntryDto>, GetAdminWalletTransactionsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetAdminWalletTransactionsQuery(walletId, limit), ct);
}
