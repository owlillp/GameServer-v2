namespace CatalogService.Domain.Authorization;

public static class CatalogPermissions
{
    public static class Catalog
    {
        public const string VIEW = "catalog.view";
        public const string MANAGE = "catalog.manage";
    }

    public static IReadOnlyList<string> All { get; } =
    [
        Catalog.VIEW,
        Catalog.MANAGE
    ];
}
