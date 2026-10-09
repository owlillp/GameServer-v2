namespace InventoryService.Contracts.Dtos;

public sealed record CarDto(
    Guid Id,
    Guid CarItemId,
    string Code,
    string? Color,
    string? Plate,
    IReadOnlyList<CarInstallationDto> Installations,
    DateTime CreatedAt);
