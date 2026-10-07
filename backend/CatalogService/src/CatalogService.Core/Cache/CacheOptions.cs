namespace CatalogService.Core.Cache;

public sealed record CacheOptions
{
    public int LocalCacheExpirationMinutes { get; init; } = 5;

    public int CacheExpirationMinutes { get; init; } = 60;

    public bool EnableRedisCache { get; init; } = true;
}
