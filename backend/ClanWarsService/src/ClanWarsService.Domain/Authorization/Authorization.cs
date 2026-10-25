namespace ClanWarsService.Domain.Authorization;

public static class ClanWarsPermissions
{
    public static class ClanWars
    {
        public const string VIEW = "clanwars.view";
        public const string MANAGE = "clanwars.manage";
    }

    public static IReadOnlyList<string> All { get; } =
    [
        ClanWars.VIEW,
        ClanWars.MANAGE,
    ];
}

public static class ClanWarsRoles
{
    public const string ADMIN = "Admin";
    public const string MODERATOR = "Moderator";
}

public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [ClanWarsRoles.ADMIN] = ClanWarsPermissions.All,
            [ClanWarsRoles.MODERATOR] = [ClanWarsPermissions.ClanWars.VIEW],
        };
}
