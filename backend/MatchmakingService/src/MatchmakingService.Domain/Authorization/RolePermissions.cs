namespace MatchmakingService.Domain.Authorization;

public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [MatchmakingRoles.ADMIN] = MatchmakingPermissions.All,
            [MatchmakingRoles.MODERATOR] = [MatchmakingPermissions.Matchmaking.VIEW, MatchmakingPermissions.Matchmaking.MANAGE],
        };
}
