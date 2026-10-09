namespace Shared.IntegrationEvents.Inventories;

public static class InventoryEventRouting
{
    public const string EXCHANGE = "inventory-events";

    public static class RoutingKeys
    {
        public const string ITEMS_GRANTED = "granted.inventory";

        public const string ALL_INVENTORY_EVENTS = "*.inventory";
    }
}
