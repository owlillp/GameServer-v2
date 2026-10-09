namespace CatalogService.Contracts.Dtos;

public sealed record AvailabilityWindowDto(
    DateTime? StartsAt,
    DateTime? EndsAt,
    string? Channel);
