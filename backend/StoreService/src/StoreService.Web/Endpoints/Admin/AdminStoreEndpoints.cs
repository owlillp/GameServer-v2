using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authorization;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;
using StoreService.Contracts.Dtos;
using StoreService.Contracts.Requests;
using StoreService.Core.Features.Offers.Commands.ActivateOffer;
using StoreService.Core.Features.Offers.Commands.ArchiveOffer;
using StoreService.Core.Features.Offers.Commands.CreateOffer;
using StoreService.Core.Features.Offers.Commands.UpdateOffer;
using StoreService.Core.Features.Offers.Queries.GetOffer;
using StoreService.Core.Features.Offers.Queries.GetOffers;
using StoreService.Core.Features.Orders.Queries.GetOrder;
using StoreService.Domain.Authorization;

namespace StoreService.Web.Endpoints.Admin;

public sealed class AdminStoreEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/admin/store";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Admin.Store")
            .RequireAuthorization();

        group.MapGet("/offers", GetOffers)
            .WithName("Admin.Store.Offers.List")
            .WithSummary("Все офферы магазина")
            .RequirePermissions(StorePermissions.Store.VIEW);

        group.MapGet("/offers/{offerId:guid}", GetOffer)
            .WithName("Admin.Store.Offers.Get")
            .WithSummary("Карточка оффера")
            .RequirePermissions(StorePermissions.Store.VIEW);

        group.MapPost("/offers", CreateOffer)
            .WithName("Admin.Store.Offers.Create")
            .WithSummary("Создать оффер")
            .RequirePermissions(StorePermissions.Store.MANAGE);

        group.MapPut("/offers/{offerId:guid}", UpdateOffer)
            .WithName("Admin.Store.Offers.Update")
            .WithSummary("Обновить оффер")
            .RequirePermissions(StorePermissions.Store.MANAGE);

        group.MapPost("/offers/{offerId:guid}/activate", ActivateOffer)
            .WithName("Admin.Store.Offers.Activate")
            .WithSummary("Активировать оффер")
            .RequirePermissions(StorePermissions.Store.MANAGE);

        group.MapDelete("/offers/{offerId:guid}", ArchiveOffer)
            .WithName("Admin.Store.Offers.Archive")
            .WithSummary("Архивировать оффер")
            .RequirePermissions(StorePermissions.Store.MANAGE);

        group.MapGet("/orders/{orderId:guid}", GetOrder)
            .WithName("Admin.Store.Orders.Get")
            .WithSummary("Карточка заказа")
            .RequirePermissions(StorePermissions.Store.VIEW);

        return group;
    }

    private static async Task<EndpointResult<IReadOnlyList<OfferDto>>> GetOffers(
        [FromServices] IQueryHandler<IReadOnlyList<OfferDto>, GetOffersQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetOffersQuery(IncludeArchived: true), ct);

    private static async Task<EndpointResult<OfferDto>> GetOffer(
        [FromRoute] Guid offerId,
        [FromServices] IQueryHandler<OfferDto, GetOfferQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetOfferQuery(offerId, IncludeUnavailable: true), ct);

    private static async Task<EndpointResult<OfferDto>> CreateOffer(
        [FromBody] CreateOfferRequest request,
        [FromServices] ICommandHandler<OfferDto, CreateOfferCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new CreateOfferCommand(request), ct);

    private static async Task<EndpointResult<OfferDto>> UpdateOffer(
        [FromRoute] Guid offerId,
        [FromBody] UpdateOfferRequest request,
        [FromServices] ICommandHandler<OfferDto, UpdateOfferCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new UpdateOfferCommand(offerId, request), ct);

    private static async Task<EndpointResult<OfferDto>> ActivateOffer(
        [FromRoute] Guid offerId,
        [FromServices] ICommandHandler<OfferDto, ActivateOfferCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new ActivateOfferCommand(offerId), ct);

    private static async Task<EndpointResult<OfferDto>> ArchiveOffer(
        [FromRoute] Guid offerId,
        [FromServices] ICommandHandler<OfferDto, ArchiveOfferCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new ArchiveOfferCommand(offerId), ct);

    private static async Task<EndpointResult<OrderDto>> GetOrder(
        [FromRoute] Guid orderId,
        [FromServices] IQueryHandler<OrderDto, GetOrderQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetOrderQuery(orderId, Guid.Empty, AllowAnyAccount: true), ct);
}
