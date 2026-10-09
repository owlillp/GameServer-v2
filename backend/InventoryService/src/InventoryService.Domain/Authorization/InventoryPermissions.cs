namespace InventoryService.Domain.Authorization;

public static class InventoryPermissions
{
    public static class Inventories
    {
        public const string VIEW = "inventory.view";
        public const string MANAGE = "inventory.manage";
    }

    public static IReadOnlyList<string> All { get; } =
    [
        Inventories.VIEW,
        Inventories.MANAGE
    ];
}
