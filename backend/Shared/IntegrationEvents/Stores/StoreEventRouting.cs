namespace Shared.IntegrationEvents.Stores;

public static class StoreEventRouting
{
    public const string COMMANDS_EXCHANGE = "store-commands";
    public const string EVENTS_EXCHANGE = "store-events";

    public static class RoutingKeys
    {
        public const string GRANT_ITEMS = "grant-items.store";

        public const string ITEMS_GRANTED = "granted.store";
        public const string ITEMS_FAILED = "failed.store";
        public const string PURCHASE_COMPLETED = "completed.store";

        public const string ALL_STORE_COMMANDS = "*.store";
        public const string ALL_STORE_EVENTS = "*.store";
    }
}
