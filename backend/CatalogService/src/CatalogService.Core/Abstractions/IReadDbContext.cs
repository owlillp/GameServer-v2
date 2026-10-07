using CatalogService.Domain.Items;
using CatalogService.Domain.Versions;

namespace CatalogService.Core.Abstractions;

public interface IReadDbContext
{
    IQueryable<Item> ItemsRead { get; }

    IQueryable<CatalogVersion> VersionsRead { get; }

    IQueryable<CatalogVersionItem> VersionItemsRead { get; }
}
