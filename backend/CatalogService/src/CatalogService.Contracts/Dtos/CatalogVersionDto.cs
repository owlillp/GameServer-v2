namespace CatalogService.Contracts.Dtos;

public sealed record CatalogVersionDto(
    Guid Id,
    long Number,
    string? Note,
    Guid? PublishedBy,
    DateTime PublishedAt,
    int ItemCount,
    string Status);
