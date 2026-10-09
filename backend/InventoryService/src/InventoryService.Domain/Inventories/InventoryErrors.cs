using Shared.SharedKernel.Errors;

namespace InventoryService.Domain.Inventories;

public static class InventoryErrors
{
    public static Error ItemNotOwned(Guid itemId) =>
        Error.Validation("inventory.item.not_owned", "Item is not owned by the player", "itemId");

    public static Error CarNotFound(Guid carId) =>
        Error.Validation("inventory.car.not_found", "Car was not found in the inventory", "carId");

    public static Error OutfitSlotRequired { get; } =
        Error.Validation("inventory.outfit.slot_required", "Outfit slot is required", "slot");

    public static Error SourceInvalid { get; } =
        Error.Validation("inventory.source.invalid", "Unknown item source", "source");

    public static Error QuantityInvalid { get; } =
        Error.Validation("inventory.quantity.invalid", "Quantity must be positive", "quantity");

    public static Error CarAlreadyExists { get; } =
        Error.Conflict("inventory.car.duplicate", "Player already owns this car model", "carItemId");

    public static Error OperationAlreadyProcessed { get; } =
        Error.Conflict("inventory.operation.duplicate", "Operation has already been processed", "operationId");
}
