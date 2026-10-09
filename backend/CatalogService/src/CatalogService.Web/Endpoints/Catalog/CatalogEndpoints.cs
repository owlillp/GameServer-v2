using CatalogService.Contracts.Dtos;
using CatalogService.Core.Features.Compatibility.Queries.GetCompatibleParts;
using CatalogService.Core.Features.Items.Queries.GetItem;
using CatalogService.Core.Features.Items.Queries.GetItems;
using CatalogService.Core.Features.Versions.Queries.GetCurrentVersion;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace CatalogService.Web.Endpoints.Catalog;

public sealed class CatalogEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/catalog";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Catalog")
            .RequireAuthorization();

        group.MapGet("/items", GetItems)
            .WithName("Catalog.Items.List")
            .WithSummary("Список опубликованных предметов каталога");

        group.MapGet("/items/{itemId:guid}", GetItem)
            .WithName("Catalog.Items.Get")
            .WithSummary("Карточка опубликованного предмета");

        group.MapGet("/items/{carItemId:guid}/compatible-parts", GetCompatibleParts)
            .WithName("Catalog.Items.CompatibleParts")
            .WithSummary("Совместимые детали для машины");

        group.MapGet("/version", GetCurrentVersion)
            .WithName("Catalog.Version.Current")
            .WithSummary("Текущая опубликованная версия каталога");

        return group;
    }

    private static async Task<EndpointResult<IReadOnlyList<ItemDto>>> GetItems(
        [FromQuery] string? kind,
        [FromQuery] string? search,
        [FromQuery] int? limit,
        [FromServices] IQueryHandler<IReadOnlyList<ItemDto>, GetItemsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetItemsQuery(kind, search, limit ?? 100), ct);

    private static async Task<EndpointResult<ItemDetailDto>> GetItem(
        [FromRoute] Guid itemId,
        [FromServices] IQueryHandler<ItemDetailDto, GetItemQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetItemQuery(itemId), ct);

    private static async Task<EndpointResult<IReadOnlyList<CompatiblePartDto>>> GetCompatibleParts(
        [FromRoute] Guid carItemId,
        [FromQuery] string? slot,
        [FromServices] IQueryHandler<IReadOnlyList<CompatiblePartDto>, GetCompatiblePartsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetCompatiblePartsQuery(carItemId, slot), ct);

    private static async Task<EndpointResult<CatalogVersionDto>> GetCurrentVersion(
        [FromServices] IQueryHandler<CatalogVersionDto, GetCurrentVersionQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetCurrentVersionQuery(), ct);
}
