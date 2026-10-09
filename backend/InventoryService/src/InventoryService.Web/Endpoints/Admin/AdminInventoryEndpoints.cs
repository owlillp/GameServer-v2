using InventoryService.Contracts.Dtos;
using InventoryService.Contracts.Requests;
using InventoryService.Core.Features.Inventories.Commands.AddCar;
using InventoryService.Core.Features.Inventories.Commands.EquipOutfit;
using InventoryService.Core.Features.Inventories.Commands.GrantItems;
using InventoryService.Core.Features.Inventories.Commands.InstallPart;
using InventoryService.Core.Features.Inventories.Commands.RemoveCar;
using InventoryService.Core.Features.Inventories.Commands.RemovePart;
using InventoryService.Core.Features.Inventories.Commands.RevokeItems;
using InventoryService.Core.Features.Inventories.Commands.UnequipOutfit;
using InventoryService.Core.Features.Inventories.Queries.GetInventory;
using InventoryService.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authorization;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace InventoryService.Web.Endpoints.Admin;

public sealed class AdminInventoryEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/admin/inventory";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Admin.Inventory")
            .RequireAuthorization();

        group.MapGet("/{accountId:guid}", GetInventory)
            .WithName("Admin.Inventory.Get")
            .WithSummary("Инвентарь игрока")
            .RequirePermissions(InventoryPermissions.Inventories.VIEW);

        group.MapPost("/{accountId:guid}/grant", GrantItems)
            .WithName("Admin.Inventory.Grant")
            .WithSummary("Выдать предметы игроку")
            .RequirePermissions(InventoryPermissions.Inventories.MANAGE);

        group.MapPost("/{accountId:guid}/revoke", RevokeItems)
            .WithName("Admin.Inventory.Revoke")
            .WithSummary("Отозвать предметы у игрока")
            .RequirePermissions(InventoryPermissions.Inventories.MANAGE);

        group.MapPut("/{accountId:guid}/outfit/{slot}", EquipOutfit)
            .WithName("Admin.Inventory.Outfit.Equip")
            .WithSummary("Надеть предмет одежды игроку")
            .RequirePermissions(InventoryPermissions.Inventories.MANAGE);

        group.MapDelete("/{accountId:guid}/outfit/{slot}", UnequipOutfit)
            .WithName("Admin.Inventory.Outfit.Unequip")
            .WithSummary("Снять предмет одежды у игрока")
            .RequirePermissions(InventoryPermissions.Inventories.MANAGE);

        group.MapPost("/{accountId:guid}/cars", AddCar)
            .WithName("Admin.Inventory.Cars.Add")
            .WithSummary("Добавить машину игроку")
            .RequirePermissions(InventoryPermissions.Inventories.MANAGE);

        group.MapDelete("/{accountId:guid}/cars/{carId:guid}", RemoveCar)
            .WithName("Admin.Inventory.Cars.Remove")
            .WithSummary("Убрать машину у игрока")
            .RequirePermissions(InventoryPermissions.Inventories.MANAGE);

        group.MapPut("/{accountId:guid}/cars/{carId:guid}/parts/{slot}", InstallPart)
            .WithName("Admin.Inventory.Cars.Parts.Install")
            .WithSummary("Установить деталь игроку")
            .RequirePermissions(InventoryPermissions.Inventories.MANAGE);

        group.MapDelete("/{accountId:guid}/cars/{carId:guid}/parts/{slot}", RemovePart)
            .WithName("Admin.Inventory.Cars.Parts.Remove")
            .WithSummary("Снять деталь у игрока")
            .RequirePermissions(InventoryPermissions.Inventories.MANAGE);

        return group;
    }

    private static async Task<EndpointResult<InventoryDto>> GetInventory(
        [FromRoute] Guid accountId,
        [FromServices] IQueryHandler<InventoryDto, GetInventoryQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetInventoryQuery(accountId), ct);

    private static async Task<EndpointResult<InventoryDto>> GrantItems(
        [FromRoute] Guid accountId,
        [FromBody] GrantItemsRequest request,
        [FromServices] ICommandHandler<InventoryDto, GrantItemsCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new GrantItemsCommand(accountId, request), ct);

    private static async Task<EndpointResult<InventoryDto>> RevokeItems(
        [FromRoute] Guid accountId,
        [FromBody] RevokeItemsRequest request,
        [FromServices] ICommandHandler<InventoryDto, RevokeItemsCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new RevokeItemsCommand(accountId, request), ct);

    private static async Task<EndpointResult<InventoryDto>> EquipOutfit(
        [FromRoute] Guid accountId,
        [FromRoute] string slot,
        [FromBody] EquipItemRequest request,
        [FromServices] ICommandHandler<InventoryDto, EquipOutfitCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new EquipOutfitCommand(accountId, slot, request.ItemId), ct);

    private static async Task<EndpointResult<InventoryDto>> UnequipOutfit(
        [FromRoute] Guid accountId,
        [FromRoute] string slot,
        [FromServices] ICommandHandler<InventoryDto, UnequipOutfitCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new UnequipOutfitCommand(accountId, slot), ct);

    private static async Task<EndpointResult<InventoryDto>> AddCar(
        [FromRoute] Guid accountId,
        [FromBody] AddCarRequest request,
        [FromServices] ICommandHandler<InventoryDto, AddCarCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new AddCarCommand(accountId, request), ct);

    private static async Task<EndpointResult<InventoryDto>> RemoveCar(
        [FromRoute] Guid accountId,
        [FromRoute] Guid carId,
        [FromServices] ICommandHandler<InventoryDto, RemoveCarCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new RemoveCarCommand(accountId, carId), ct);

    private static async Task<EndpointResult<InventoryDto>> InstallPart(
        [FromRoute] Guid accountId,
        [FromRoute] Guid carId,
        [FromRoute] string slot,
        [FromBody] InstallPartRequest request,
        [FromServices] ICommandHandler<InventoryDto, InstallPartCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new InstallPartCommand(accountId, carId, slot, request.PartItemId), ct);

    private static async Task<EndpointResult<InventoryDto>> RemovePart(
        [FromRoute] Guid accountId,
        [FromRoute] Guid carId,
        [FromRoute] string slot,
        [FromServices] ICommandHandler<InventoryDto, RemovePartCommand> handler,
        CancellationToken ct) =>
        await handler.Handle(new RemovePartCommand(accountId, carId, slot), ct);
}
