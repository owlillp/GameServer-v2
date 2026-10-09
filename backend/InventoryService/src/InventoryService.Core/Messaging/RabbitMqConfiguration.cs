using Shared.IntegrationEvents.Identity;
using Shared.IntegrationEvents.Inventories;
using Shared.IntegrationEvents.Inventories.Events;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace InventoryService.Core.Messaging;

public static class RabbitMqConfiguration
{
    private const string ACCOUNT_EVENTS_QUEUE = "inventory.account-events";

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
                });

            options.ConfigureInventoryEventPublishing();
            options.ConfigureAccountEventsListeners();
        }

        private void ConfigureInventoryEventPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<ItemsGrantedEvent>(
                    InventoryEventRouting.EXCHANGE,
                    _ => InventoryEventRouting.RoutingKeys.ITEMS_GRANTED)
                .UseDurableOutbox();
        }

        private void ConfigureAccountEventsListeners()
        {
            options.ListenToRabbitQueue(ACCOUNT_EVENTS_QUEUE, queue =>
                queue.BindExchange(IdentityEventRouting.EXCHANGE, IdentityEventRouting.RoutingKeys.ALL_ACCOUNT_EVENTS));
        }
    }
}
