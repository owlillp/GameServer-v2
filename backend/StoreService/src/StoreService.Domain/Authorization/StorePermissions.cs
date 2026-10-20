namespace StoreService.Domain.Authorization;

public static class StorePermissions
{
    public static class Store
    {
        public const string VIEW = "store.view";
        public const string MANAGE = "store.manage";
    }

    public static IReadOnlyList<string> All { get; } =
    [
        Store.VIEW,
        Store.MANAGE,
    ];
}
