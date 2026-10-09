using Microsoft.Extensions.Caching.Hybrid;

namespace CatalogService.Core.Cache;

public sealed class CatalogCacheInvalidator(HybridCache cache)
{
    public async Task InvalidateItemAsync(Guid itemId, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(CatalogCacheKeys.Item(itemId), cancellationToken);
        await InvalidateListsAsync(cancellationToken);
    }

    public async Task InvalidateListsAsync(CancellationToken cancellationToken) =>
        await cache.RemoveByTagAsync(CatalogCacheKeys.ListTag, cancellationToken);

    public async Task InvalidateVersionAsync(CancellationToken cancellationToken) =>
        await cache.RemoveByTagAsync(CatalogCacheKeys.VersionTag, cancellationToken);

    public async Task InvalidateAllAsync(CancellationToken cancellationToken)
    {
        await InvalidateListsAsync(cancellationToken);
        await InvalidateVersionAsync(cancellationToken);
    }
}
