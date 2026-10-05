namespace WalletService.Domain.Authorization;

// Значения должны совпадать с ролями из токена (AuthService.Domain.Authorization.AuthRoles).
public static class WalletRoles
{
    public const string ADMIN = "Admin";
    public const string MODERATOR = "Moderator";
}
