namespace ClanService.Domain.Authorization;

public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [ClanRoles.ADMIN] = ClanPermissions.All,
            [ClanRoles.MODERATOR] = [ClanPermissions.Clans.VIEW],
        };
}
