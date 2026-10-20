namespace StoreService.Domain.Authorization;

public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [StoreRoles.ADMIN] = StorePermissions.All,
            [StoreRoles.MODERATOR] = [StorePermissions.Store.VIEW, StorePermissions.Store.MANAGE],
        };
}
