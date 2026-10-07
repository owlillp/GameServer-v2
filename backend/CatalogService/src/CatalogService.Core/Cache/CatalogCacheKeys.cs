using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CatalogService.Core.Cache;

public static class CatalogCacheKeys
{
    public const string ItemPrefix = "catalog:item";

    public const string ListPrefix = "catalog:list";

    public const string ListTag = "catalog:list";

    public const string VersionTag = "catalog:version";

    private const string Version = "v1";

    public static string Item(Guid itemId) => $"{ItemPrefix}:{Version}:{itemId}";

    public static string List(string? kind, string? search, int limit)
    {
        string canonical = string.Join(
            '\x1f',
            kind?.Trim().ToLowerInvariant() ?? "all",
            search?.Trim().ToLowerInvariant() ?? "-",
            limit.ToString(CultureInfo.InvariantCulture));

        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))[..32].ToLowerInvariant();
        return $"{ListPrefix}:{Version}:{hash}";
    }
}
