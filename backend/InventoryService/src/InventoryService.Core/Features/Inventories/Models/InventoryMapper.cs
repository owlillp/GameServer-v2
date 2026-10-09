using InventoryService.Contracts.Dtos;
using InventoryService.Domain.Inventories;

namespace InventoryService.Core.Features.Inventories.Models;

internal static class InventoryMapper
{
    public static InventoryDto ToDto(Inventory inventory) =>
        new(
            inventory.AccountId,
            [.. inventory.Items.Select(ToItemDto)],
            [.. inventory.Outfit.Select(entry => new OutfitEntryDto(entry.Slot, entry.ItemId))],
            [.. inventory.Cars.Select(ToCarDto)],
            inventory.CreatedAt,
            inventory.UpdatedAt);

    public static OwnedItemDto ToItemDto(OwnedItem item) =>
        new(item.ItemId, item.Code, item.Kind, item.Slot, item.Source, item.Quantity, item.AcquiredAt);

    public static CarDto ToCarDto(OwnedCar car) =>
        new(
            car.Id,
            car.CarItemId,
            car.Code,
            car.Color,
            car.Plate,
            [.. car.Installations.Select(ToInstallationDto)],
            car.CreatedAt);

    public static CarInstallationDto ToInstallationDto(CarInstallation installation) =>
        new(installation.Slot, installation.PartItemId);
}
