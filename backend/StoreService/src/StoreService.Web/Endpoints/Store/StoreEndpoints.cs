using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;
using StoreService.Contracts.Dtos;
using StoreService.Contracts.Requests;
using StoreService.Core.Features.Offers.Queries.GetOffer;
using StoreService.Core.Features.Offers.Queries.GetOffers;
using StoreService.Core.Features.Orders.Commands.PurchaseOffer;
using StoreService.Core.Features.Orders.Queries.GetMyOrders;
using StoreService.Core.Features.Orders.Queries.GetOrder;

namespace StoreService.Web.Endpoints.Store;

public sealed class StoreEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/store";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Store")
            .RequireAuthorization();

        group.MapGet("/offers", GetOffers)
            .WithName("Store.Offers.List")
            .WithSummary("Активные офферы магазина");

        group.MapGet("/offers/{offerId:guid}", GetOffer)
            .WithName("Store.Offers.Get")
            .WithSummary("Карточка активного оффера");

        group.MapPost("/purchases", Purchase)
            .WithName("Store.Purchases.Create")
            .WithSummary("Купить оффер (асинхронная выдача предметов)");

        group.MapGet("/orders", GetMyOrders)
            .WithName("Store.Orders.List")
            .WithSummary("История покупок текущего игрока");

        group.MapGet("/orders/{orderId:guid}", GetOrder)
            .WithName("Store.Orders.Get")
            .WithSummary("Статус покупки текущего игрока");

        return group;
    }

    private static async Task<EndpointResult<IReadOnlyList<OfferDto>>> GetOffers(
        [FromServices] IQueryHandler<IReadOnlyList<OfferDto>, GetOffersQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetOffersQuery(IncludeArchived: false), ct);

    private static async Task<EndpointResult<OfferDto>> GetOffer(
        [FromRoute] Guid offerId,
        [FromServices] IQueryHandler<OfferDto, GetOfferQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetOfferQuery(offerId, IncludeUnavailable: false), ct);

    private static async Task<EndpointResult<OrderDto>> Purchase(
        [FromBody] PurchaseOfferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromServices] ICommandHandler<OrderDto, PurchaseOfferCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(
            new PurchaseOfferCommand(userScope.Id ?? Guid.Empty, idempotencyKey, request),
            ct);

    private static async Task<EndpointResult<IReadOnlyList<OrderDto>>> GetMyOrders(
        [FromQuery] int? limit,
        [FromServices] IQueryHandler<IReadOnlyList<OrderDto>, GetMyOrdersQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new GetMyOrdersQuery(userScope.Id ?? Guid.Empty, limit ?? 50), ct);

    private static async Task<EndpointResult<OrderDto>> GetOrder(
        [FromRoute] Guid orderId,
        [FromServices] IQueryHandler<OrderDto, GetOrderQuery> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new GetOrderQuery(orderId, userScope.Id ?? Guid.Empty), ct);
}
