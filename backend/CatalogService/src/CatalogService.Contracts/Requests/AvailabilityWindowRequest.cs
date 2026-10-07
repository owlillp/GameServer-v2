namespace CatalogService.Contracts.Requests;

public sealed record AvailabilityWindowRequest(DateTime? StartsAt, DateTime? EndsAt, string? Channel);
