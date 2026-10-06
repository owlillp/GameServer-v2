namespace Shared.IntegrationEvents.Wallets;

public static class WalletEventRouting
{
    public const string EXCHANGE = "wallet-events";

    public static class RoutingKeys
    {
        public const string CREDITED = "credited.wallet";
        public const string DEBITED = "debited.wallet";

        public const string ALL_WALLET_EVENTS = "*.wallet";
    }
}
