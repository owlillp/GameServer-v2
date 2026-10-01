using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authorization;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;
using Shared.SharedKernel.Responses;
using TemplateService.Contracts.Items;
using TemplateService.Core.Authorization;
using TemplateService.Core.Features.Items.Commands.CreateItem;
using TemplateService.Core.Features.Items.Commands.DeleteItem;
using TemplateService.Core.Features.Items.Commands.UpdateItem;
using TemplateService.Core.Features.Items.Queries.GetItem;
using TemplateService.Core.Features.Items.Queries.ListItems;

namespace TemplateService.Web.Endpoints.Items;

public sealed class ItemsEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/items";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Items")
            .RequireAuthorization();

        group.MapGet("", List)
            .WithName("Items.List")
            .WithSummary("Список записей с поиском и пагинацией")
            .RequirePermissions(TemplatePermissions.ITEMS_READ);

        group.MapGet("/{itemId:guid}", Get)
            .WithName("Items.Get")
            .WithSummary("Запись по идентификатору")
            .RequirePermissions(TemplatePermissions.ITEMS_READ);

        group.MapPost("", Create)
            .WithName("Items.Create")
            .WithSummary("Создать запись; владелец — текущий пользователь")
            .RequirePermissions(TemplatePermissions.ITEMS_WRITE);

        group.MapPut("/{itemId:guid}", Update)
            .WithName("Items.Update")
            .WithSummary("Обновить запись (владелец или items.manage)")
            .RequirePermissions(TemplatePermissions.ITEMS_WRITE);

        group.MapDelete("/{itemId:guid}", Delete)
            .WithName("Items.Delete")
            .WithSummary("Удалить запись (владелец или items.manage)")
            .RequirePermissions(TemplatePermissions.ITEMS_WRITE);

        return group;
    }

    private static async Task<EndpointResult<PaginationResponse<ItemDto>>> List(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? search,
        [FromServices] IQueryHandler<PaginationResponse<ItemDto>, ListItemsQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new ListItemsQuery(page, pageSize, search), ct);

    private static async Task<EndpointResult<ItemDto>> Get(
        [FromRoute] Guid itemId,
        [FromServices] IQueryHandler<ItemDto, GetItemQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetItemQuery(itemId), ct);

    private static async Task<EndpointResult<ItemDto>> Create(
        [FromBody] CreateItemRequest request,
        [FromServices] ICommandHandler<ItemDto, CreateItemCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new CreateItemCommand(request), ct);

    private static async Task<EndpointResult<ItemDto>> Update(
        [FromRoute] Guid itemId,
        [FromBody] UpdateItemRequest request,
        [FromServices] ICommandHandler<ItemDto, UpdateItemCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new UpdateItemCommand(itemId, request), ct);

    private static async Task<EndpointResult> Delete(
        [FromRoute] Guid itemId,
        [FromServices] ICommandHandler<DeleteItemCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new DeleteItemCommand(itemId), ct);
}
