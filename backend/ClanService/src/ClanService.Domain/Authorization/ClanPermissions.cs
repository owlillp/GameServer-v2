namespace ClanService.Domain.Authorization;

public static class ClanPermissions
{
    public static class Clans
    {
        public const string VIEW = "clans.view";
        public const string MANAGE = "clans.manage";
    }

    public static IReadOnlyList<string> All { get; } =
    [
        Clans.VIEW,
        Clans.MANAGE,
    ];
}
