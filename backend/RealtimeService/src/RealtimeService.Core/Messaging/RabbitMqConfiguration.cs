using Shared.IntegrationEvents.Events;
using Shared.IntegrationEvents.Inventories;
using Shared.IntegrationEvents.Wallets;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace RealtimeService.Core.Messaging;

public static class RabbitMqConfiguration
{
    private const string EVENT_EVENTS_QUEUE = "realtime.event-events";
    private const string WALLET_EVENTS_QUEUE = "realtime.wallet-events";
    private const string INVENTORY_EVENTS_QUEUE = "realtime.inventory-events";

    extension(WolverineOptions options)
    {
        public void ConfigureRabbitMq(string connectionString)
        {
            options.UseRabbitMq(new Uri(connectionString))
                .AutoProvision()
                .EnableWolverineControlQueues()
                .UseQuorumQueues()
                .UsePlatformChannelDefaults()
                .DeclareExchange(EventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(WalletEventRouting.EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(InventoryEventRouting.EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                });

            options.ConfigureListeners();
        }

        private void ConfigureListeners()
        {
            options.ListenToRabbitQueue(EVENT_EVENTS_QUEUE, queue =>
            {
                queue.BindExchange(EventRouting.EVENTS_EXCHANGE, EventRouting.RoutingKeys.STARTED);
                queue.BindExchange(EventRouting.EVENTS_EXCHANGE, EventRouting.RoutingKeys.ENDED);
                queue.BindExchange(EventRouting.EVENTS_EXCHANGE, EventRouting.RoutingKeys.REWARD_GRANTED);
            });

            options.ListenToRabbitQueue(WALLET_EVENTS_QUEUE, queue =>
            {
                queue.BindExchange(WalletEventRouting.EXCHANGE, WalletEventRouting.RoutingKeys.CREDITED);
                queue.BindExchange(WalletEventRouting.EXCHANGE, WalletEventRouting.RoutingKeys.DEBITED);
            });

            options.ListenToRabbitQueue(INVENTORY_EVENTS_QUEUE, queue =>
                queue.BindExchange(
                    InventoryEventRouting.EXCHANGE,
                    InventoryEventRouting.RoutingKeys.ITEMS_GRANTED));
        }
    }
}
