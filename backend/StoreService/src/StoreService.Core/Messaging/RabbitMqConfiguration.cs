using Shared.IntegrationEvents.Stores;
using Shared.IntegrationEvents.Stores.Commands;
using Shared.IntegrationEvents.Stores.Events;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace StoreService.Core.Messaging;

public static class RabbitMqConfiguration
{
    private const string PURCHASE_EVENTS_QUEUE = "store.purchase-events";

    extension(WolverineOptions options)
    {
        public void ConfigureRabbitMq(string connectionString)
        {
            options.UseRabbitMq(new Uri(connectionString))
                .AutoProvision()
                .EnableWolverineControlQueues()
                .UseQuorumQueues()
                .UsePlatformChannelDefaults()
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

            options.ConfigureCommandPublishing();
            options.ConfigureEventPublishing();
            options.ConfigurePurchaseEventListeners();
        }

        private void ConfigureCommandPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<GrantPurchaseItemsCommand>(
                    StoreEventRouting.COMMANDS_EXCHANGE,
                    _ => StoreEventRouting.RoutingKeys.GRANT_ITEMS)
                .UseDurableOutbox();
        }

        private void ConfigureEventPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<PurchaseCompletedEvent>(
                    StoreEventRouting.EVENTS_EXCHANGE,
                    _ => StoreEventRouting.RoutingKeys.PURCHASE_COMPLETED)
                .UseDurableOutbox();
        }

        private void ConfigurePurchaseEventListeners()
        {
            options.ListenToRabbitQueue(PURCHASE_EVENTS_QUEUE, queue =>
            {
                queue.BindExchange(StoreEventRouting.EVENTS_EXCHANGE, StoreEventRouting.RoutingKeys.ITEMS_GRANTED);
                queue.BindExchange(StoreEventRouting.EVENTS_EXCHANGE, StoreEventRouting.RoutingKeys.ITEMS_FAILED);
            });
        }
    }
}
