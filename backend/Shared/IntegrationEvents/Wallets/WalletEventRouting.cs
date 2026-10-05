namespace Shared.IntegrationEvents.Wallets;

public static class WalletEventRouting
{
    public const string EXCHANGE = "wallet-events";

    public static class RoutingKeys
    {
        public const string TOP_UP = "top_up.wallet";
        public const string SPENT = "spent.wallet";

        public const string ALL_WALLET_EVENTS = "*.wallet";
    }
}
