using Shared.IntegrationEvents.Identity;
using Shared.IntegrationEvents.Inventories;
using Shared.IntegrationEvents.Inventories.Events;
using Shared.IntegrationEvents.Stores;
using Shared.IntegrationEvents.Stores.Events;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace InventoryService.Core.Messaging;

public static class RabbitMqConfiguration
{
    private const string ACCOUNT_EVENTS_QUEUE = "inventory.account-events";
    private const string STORE_COMMANDS_QUEUE = "inventory.store-commands";

    extension(WolverineOptions options)
    {
        public void ConfigureRabbitMq(string connectionString)
        {
            options.UseRabbitMq(new Uri(connectionString))
                .AutoProvision()
                .EnableWolverineControlQueues()
                .UseQuorumQueues()
                .UsePlatformChannelDefaults()
                .DeclareExchange(IdentityEventRouting.EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(InventoryEventRouting.EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(StoreEventRouting.COMMANDS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(StoreEventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                });

            options.ConfigureInventoryEventPublishing();
            options.ConfigureStoreEventPublishing();
            options.ConfigureAccountEventsListeners();
            options.ConfigureStoreCommandListeners();
        }

        private void ConfigureInventoryEventPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<ItemsGrantedEvent>(
                    InventoryEventRouting.EXCHANGE,
                    _ => InventoryEventRouting.RoutingKeys.ITEMS_GRANTED)
                .UseDurableOutbox();
        }

        private void ConfigureStoreEventPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<PurchaseItemsGrantedEvent>(
                    StoreEventRouting.EVENTS_EXCHANGE,
                    _ => StoreEventRouting.RoutingKeys.ITEMS_GRANTED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<PurchaseItemsFailedEvent>(
                    StoreEventRouting.EVENTS_EXCHANGE,
                    _ => StoreEventRouting.RoutingKeys.ITEMS_FAILED)
                .UseDurableOutbox();
        }

        private void ConfigureAccountEventsListeners()
        {
            options.ListenToRabbitQueue(ACCOUNT_EVENTS_QUEUE, queue =>
                queue.BindExchange(IdentityEventRouting.EXCHANGE, IdentityEventRouting.RoutingKeys.ALL_ACCOUNT_EVENTS));
        }

        private void ConfigureStoreCommandListeners()
        {
            options.ListenToRabbitQueue(STORE_COMMANDS_QUEUE, queue =>
                queue.BindExchange(StoreEventRouting.COMMANDS_EXCHANGE, StoreEventRouting.RoutingKeys.GRANT_ITEMS));
        }
    }
}
