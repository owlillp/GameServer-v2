namespace CatalogService.Contracts.Dtos;

public sealed record CatalogVersionDetailDto(
    Guid Id,
    long Number,
    string? Note,
    Guid? PublishedBy,
    DateTime PublishedAt,
    int ItemCount,
    string Status,
    IReadOnlyList<CatalogVersionItemDto> Items);
