namespace WalletService.Domain.Authorization;

public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Map =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [WalletRoles.ADMIN] = WalletPermissions.All,
            [WalletRoles.MODERATOR] = [WalletPermissions.Wallets.MANAGE, WalletPermissions.Wallets.VIEW],
        };
}
