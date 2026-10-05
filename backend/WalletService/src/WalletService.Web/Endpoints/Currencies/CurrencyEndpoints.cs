using Microsoft.AspNetCore.Mvc;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;
using WalletService.Contracts.Dtos;
using WalletService.Core.Features.Currencies.Queries.GetCurrencies;

namespace WalletService.Web.Endpoints.Currencies;

public sealed class CurrencyEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/wallets/currencies";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Currencies")
            .RequireAuthorization();

        group.MapGet(string.Empty, GetCurrencies)
            .WithName("Currencies.List")
            .WithSummary("Список доступных валют");

        return group;
    }

    private static async Task<EndpointResult<IReadOnlyList<CurrencyDto>>> GetCurrencies(
        [FromServices] IQueryHandler<IReadOnlyList<CurrencyDto>, GetCurrenciesQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetCurrenciesQuery(), ct);
}
