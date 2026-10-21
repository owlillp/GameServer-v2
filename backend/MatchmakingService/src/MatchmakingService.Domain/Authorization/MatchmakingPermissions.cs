namespace MatchmakingService.Domain.Authorization;

public static class MatchmakingPermissions
{
    public static class Matchmaking
    {
        public const string VIEW = "matchmaking.view";
        public const string MANAGE = "matchmaking.manage";
    }

    public static IReadOnlyList<string> All { get; } =
    [
        Matchmaking.VIEW,
        Matchmaking.MANAGE,
    ];
}
