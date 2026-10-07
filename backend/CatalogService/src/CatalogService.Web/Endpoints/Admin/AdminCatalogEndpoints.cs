using CatalogService.Contracts.Dtos;
using CatalogService.Contracts.Requests;
using CatalogService.Core.Features.Items.Commands.ArchiveItem;
using CatalogService.Core.Features.Items.Commands.CreateItem;
using CatalogService.Core.Features.Items.Commands.UpdateItem;
using CatalogService.Core.Features.Items.Queries.GetAdminItems;
using CatalogService.Core.Features.Versions.Commands.PublishVersion;
using CatalogService.Core.Features.Versions.Commands.RollbackVersion;
using CatalogService.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authentication.UserScope;
using Shared.Authentication.Authorization;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace CatalogService.Web.Endpoints.Admin;

public sealed class AdminCatalogEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/admin/catalog";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Admin.Catalog")
            .RequireAuthorization();

        group.MapGet("/items", GetItems)
            .WithName("Admin.Catalog.Items.List")
            .WithSummary("Список предметов каталога (включая черновики)")
            .RequirePermissions(CatalogPermissions.Catalog.VIEW);

        group.MapPost("/items", CreateItem)
            .WithName("Admin.Catalog.Items.Create")
            .WithSummary("Создать предмет")
            .RequirePermissions(CatalogPermissions.Catalog.MANAGE);

        group.MapPut("/items/{itemId:guid}", UpdateItem)
            .WithName("Admin.Catalog.Items.Update")
            .WithSummary("Обновить предмет")
            .RequirePermissions(CatalogPermissions.Catalog.MANAGE);

        group.MapDelete("/items/{itemId:guid}", ArchiveItem)
            .WithName("Admin.Catalog.Items.Archive")
            .WithSummary("Архивировать предмет")
            .RequirePermissions(CatalogPermissions.Catalog.MANAGE);

        group.MapPost("/versions", PublishVersion)
            .WithName("Admin.Catalog.Versions.Publish")
            .WithSummary("Опубликовать новую версию каталога")
            .RequirePermissions(CatalogPermissions.Catalog.MANAGE);

        group.MapPost("/versions/rollback", RollbackVersion)
            .WithName("Admin.Catalog.Versions.Rollback")
            .WithSummary("Откатить каталог к версии")
            .RequirePermissions(CatalogPermissions.Catalog.MANAGE);

        return group;
    }

    private static async Task<EndpointResult<IReadOnlyList<ItemDetailDto>>> GetItems(
        [FromQuery] string? status,
        [FromQuery] string? kind,
        [FromServices] IQueryHandler<IReadOnlyList<ItemDetailDto>, GetAdminItemsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetAdminItemsQuery(status, kind), ct);

    private static async Task<EndpointResult<ItemDetailDto>> CreateItem(
        [FromBody] CreateItemRequest request,
        [FromServices] ICommandHandler<ItemDetailDto, CreateItemCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new CreateItemCommand(request), ct);

    private static async Task<EndpointResult<ItemDetailDto>> UpdateItem(
        [FromRoute] Guid itemId,
        [FromBody] UpdateItemRequest request,
        [FromServices] ICommandHandler<ItemDetailDto, UpdateItemCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new UpdateItemCommand(itemId, request), ct);

    private static async Task<EndpointResult<ItemDetailDto>> ArchiveItem(
        [FromRoute] Guid itemId,
        [FromServices] ICommandHandler<ItemDetailDto, ArchiveItemCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new ArchiveItemCommand(itemId), ct);

    private static async Task<EndpointResult<CatalogVersionDto>> PublishVersion(
        [FromBody] PublishVersionRequest request,
        [FromServices] ICommandHandler<CatalogVersionDto, PublishVersionCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new PublishVersionCommand(request, userScope.Id), ct);

    private static async Task<EndpointResult<CatalogVersionDto>> RollbackVersion(
        [FromBody] RollbackVersionRequest request,
        [FromServices] ICommandHandler<CatalogVersionDto, RollbackVersionCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new RollbackVersionCommand(request, userScope.Id), ct);
}
