namespace SessionService.Domain.Authorization;

public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [SessionRoles.ADMIN] = SessionPermissions.All,
            [SessionRoles.MODERATOR] = [SessionPermissions.Sessions.VIEW],
        };
}
