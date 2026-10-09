namespace CatalogService.Domain.Authorization;

public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [CatalogRoles.ADMIN] = CatalogPermissions.All,
            [CatalogRoles.MODERATOR] = [CatalogPermissions.Catalog.VIEW, CatalogPermissions.Catalog.MANAGE],
        };
}
