namespace EventService.Domain.Authorization;

public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [EventRoles.ADMIN] = EventPermissions.All,
            [EventRoles.MODERATOR] = [EventPermissions.Events.VIEW, EventPermissions.Events.MANAGE],
        };
}
