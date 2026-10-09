namespace InventoryService.Domain.Authorization;

public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [InventoryRoles.ADMIN] = InventoryPermissions.All,
            [InventoryRoles.MODERATOR] = [InventoryPermissions.Inventories.VIEW, InventoryPermissions.Inventories.MANAGE],
        };
}
