namespace InventoryService.Contracts.Requests;

public sealed record AddCarRequest(
    Guid CarItemId,
    string Code,
    string? Color,
    string? Plate);
