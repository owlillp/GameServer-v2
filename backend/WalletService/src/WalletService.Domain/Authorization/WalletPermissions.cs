namespace WalletService.Domain.Authorization;

public static class WalletPermissions
{
    public static class Wallets
    {
        public const string VIEW = "wallets.view";
        public const string MANAGE = "wallets.manage";
    }

    public static IReadOnlyList<string> All { get; } = [Wallets.VIEW, Wallets.MANAGE];
}
