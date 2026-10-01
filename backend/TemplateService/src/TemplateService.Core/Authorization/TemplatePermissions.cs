namespace TemplateService.Core.Authorization;

// Permission-константы сервиса. Permissions попадают в политики авторизации
// Shared.Authentication: endpoint объявляет .RequirePermissions(...), а роли
// сопоставляются с permissions через TemplateRolePermissions.Map.
public static class TemplatePermissions
{
    public const string ITEMS_READ = "items.read";

    public const string ITEMS_WRITE = "items.write";

    // Модерация чужих записей.
    public const string ITEMS_MANAGE = "items.manage";
}

// Имена ролей совпадают с claim "role" в access-токенах AuthService.
public static class TemplateAuthRoles
{
    public const string USER = "User";
    public const string MODERATOR = "Moderator";
    public const string ADMIN = "Admin";
    public const string SERVICE = "Service";
}

public static class TemplateRolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [TemplateAuthRoles.USER] =
            [
                TemplatePermissions.ITEMS_READ,
                TemplatePermissions.ITEMS_WRITE,
            ],
            [TemplateAuthRoles.MODERATOR] =
            [
                TemplatePermissions.ITEMS_READ,
                TemplatePermissions.ITEMS_WRITE,
                TemplatePermissions.ITEMS_MANAGE,
            ],
            [TemplateAuthRoles.ADMIN] =
            [
                TemplatePermissions.ITEMS_READ,
                TemplatePermissions.ITEMS_WRITE,
                TemplatePermissions.ITEMS_MANAGE,
            ],
            [TemplateAuthRoles.SERVICE] =
            [
                TemplatePermissions.ITEMS_READ,
            ],
        };
}
