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
using InventoryService.Core.Features.Inventories.Queries.GetMyInventory;
using Microsoft.AspNetCore.Mvc;
using Shared.Authentication.Authentication.UserScope;
using Shared.Core.Abstractions;
using Shared.Framework.Endpoints;

namespace InventoryService.Web.Endpoints.Inventories;

public sealed class InventoryEndpoints : IEndpointGroup
{
    private const string RESOURCE = "/inventory";

    public RouteGroupBuilder MapGroup(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup(RESOURCE)
            .WithTags("Inventory")
            .RequireAuthorization();

        group.MapGet("/me", GetMyInventory)
            .WithName("Inventory.Me.Get")
            .WithSummary("Инвентарь текущего игрока");

        group.MapPut("/outfit/{slot}", EquipOutfit)
            .WithName("Inventory.Outfit.Equip")
            .WithSummary("Надеть предмет одежды в слот");

        group.MapDelete("/outfit/{slot}", UnequipOutfit)
            .WithName("Inventory.Outfit.Unequip")
            .WithSummary("Снять предмет со слота");

        group.MapPost("/cars", AddCar)
            .WithName("Inventory.Cars.Add")
            .WithSummary("Добавить машину в гараж");

        group.MapDelete("/cars/{carId:guid}", RemoveCar)
            .WithName("Inventory.Cars.Remove")
            .WithSummary("Убрать машину из гаража");

        group.MapPut("/cars/{carId:guid}/parts/{slot}", InstallPart)
            .WithName("Inventory.Cars.Parts.Install")
            .WithSummary("Установить деталь тюнинга");

        group.MapDelete("/cars/{carId:guid}/parts/{slot}", RemovePart)
            .WithName("Inventory.Cars.Parts.Remove")
            .WithSummary("Снять деталь тюнинга");

        group.MapPost("/{accountId:guid}/grant", GrantItems)
            .WithName("Inventory.Grant")
            .WithSummary("Выдача предметов игроку (Store/награды, идемпотентно)");

        group.MapPost("/{accountId:guid}/revoke", RevokeItems)
            .WithName("Inventory.Revoke")
            .WithSummary("Отзыв предметов у игрока (возвраты/компенсации)");

        return group;
    }

    private static async Task<EndpointResult<InventoryDto>> GetMyInventory(
        [FromServices] IQueryHandler<InventoryDto, GetMyInventoryQuery> handler,
        CancellationToken ct) =>
        await handler.Handle(new GetMyInventoryQuery(), ct);

    private static async Task<EndpointResult<InventoryDto>> EquipOutfit(
        [FromRoute] string slot,
        [FromBody] EquipItemRequest request,
        [FromServices] ICommandHandler<InventoryDto, EquipOutfitCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new EquipOutfitCommand(userScope.Id ?? Guid.Empty, slot, request.ItemId), ct);

    private static async Task<EndpointResult<InventoryDto>> UnequipOutfit(
        [FromRoute] string slot,
        [FromServices] ICommandHandler<InventoryDto, UnequipOutfitCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new UnequipOutfitCommand(userScope.Id ?? Guid.Empty, slot), ct);

    private static async Task<EndpointResult<InventoryDto>> AddCar(
        [FromBody] AddCarRequest request,
        [FromServices] ICommandHandler<InventoryDto, AddCarCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new AddCarCommand(userScope.Id ?? Guid.Empty, request), ct);

    private static async Task<EndpointResult<InventoryDto>> RemoveCar(
        [FromRoute] Guid carId,
        [FromServices] ICommandHandler<InventoryDto, RemoveCarCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new RemoveCarCommand(userScope.Id ?? Guid.Empty, carId), ct);

    private static async Task<EndpointResult<InventoryDto>> InstallPart(
        [FromRoute] Guid carId,
        [FromRoute] string slot,
        [FromBody] InstallPartRequest request,
        [FromServices] ICommandHandler<InventoryDto, InstallPartCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new InstallPartCommand(userScope.Id ?? Guid.Empty, carId, slot, request.PartItemId), ct);

    private static async Task<EndpointResult<InventoryDto>> RemovePart(
        [FromRoute] Guid carId,
        [FromRoute] string slot,
        [FromServices] ICommandHandler<InventoryDto, RemovePartCommand> handler,
        [FromServices] UserScopedData userScope,
        CancellationToken ct) =>
        await handler.Handle(new RemovePartCommand(userScope.Id ?? Guid.Empty, carId, slot), ct);

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
}
