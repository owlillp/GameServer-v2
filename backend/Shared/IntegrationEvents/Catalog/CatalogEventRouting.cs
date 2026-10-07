namespace Shared.IntegrationEvents.Catalog;

public static class CatalogEventRouting
{
    public const string EXCHANGE = "catalog-events";

    public static class RoutingKeys
    {
        public const string VERSION_PUBLISHED = "published.catalog";

        public const string ALL_CATALOG_EVENTS = "*.catalog";
    }
}
