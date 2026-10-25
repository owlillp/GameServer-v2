using Shared.IntegrationEvents.Events;
using Shared.IntegrationEvents.Events.Commands;
using Shared.IntegrationEvents.Events.Events;
using Shared.IntegrationEvents.Identity;
using Shared.IntegrationEvents.Inventories;
using Shared.IntegrationEvents.Inventories.Events;
using Shared.IntegrationEvents.ClanWars;
using Shared.IntegrationEvents.ClanWars.Commands;
using Shared.IntegrationEvents.ClanWars.Events;
using Shared.IntegrationEvents.Matchmaking;
using Shared.IntegrationEvents.Matchmaking.Commands;
using Shared.IntegrationEvents.Matchmaking.Events;
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
    private const string EVENT_COMMANDS_QUEUE = "inventory.event-commands";
    private const string MATCHMAKING_COMMANDS_QUEUE = "inventory.matchmaking-commands";
    private const string CLANWARS_COMMANDS_QUEUE = "inventory.clanwars-commands";

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
                })
                .DeclareExchange(EventRouting.COMMANDS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(EventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(MatchmakingEventRouting.COMMANDS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(ClanWarsEventRouting.COMMANDS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(ClanWarsEventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(MatchmakingEventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                });

            options.ConfigureInventoryEventPublishing();
            options.ConfigureStoreEventPublishing();
            options.ConfigureEventRewardPublishing();
            options.ConfigureMatchRewardPublishing();

            options.PublishMessagesToRabbitMqExchange<SeasonRewardItemsGrantedEvent>(
                    ClanWarsEventRouting.EVENTS_EXCHANGE,
                    _ => ClanWarsEventRouting.RoutingKeys.REWARD_ITEMS_GRANTED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<SeasonRewardItemsFailedEvent>(
                    ClanWarsEventRouting.EVENTS_EXCHANGE,
                    _ => ClanWarsEventRouting.RoutingKeys.REWARD_ITEMS_FAILED)
                .UseDurableOutbox();

            options.ListenToRabbitQueue(CLANWARS_COMMANDS_QUEUE, queue =>
                queue.BindExchange(ClanWarsEventRouting.COMMANDS_EXCHANGE, ClanWarsEventRouting.RoutingKeys.GRANT_ITEMS));
            options.ConfigureAccountEventsListeners();
            options.ConfigureStoreCommandListeners();
            options.ConfigureEventCommandListeners();
            options.ConfigureMatchmakingCommandListeners();
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

        private void ConfigureEventRewardPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<EventRewardGrantedEvent>(
                    EventRouting.EVENTS_EXCHANGE,
                    _ => EventRouting.RoutingKeys.REWARD_GRANTED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<EventRewardGrantFailedEvent>(
                    EventRouting.EVENTS_EXCHANGE,
                    _ => EventRouting.RoutingKeys.REWARD_FAILED)
                .UseDurableOutbox();
        }

        private void ConfigureMatchRewardPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<MatchRewardGrantedEvent>(
                    MatchmakingEventRouting.EVENTS_EXCHANGE,
                    _ => MatchmakingEventRouting.RoutingKeys.REWARD_GRANTED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<MatchRewardGrantFailedEvent>(
                    MatchmakingEventRouting.EVENTS_EXCHANGE,
                    _ => MatchmakingEventRouting.RoutingKeys.REWARD_FAILED)
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

        private void ConfigureEventCommandListeners()
        {
            options.ListenToRabbitQueue(EVENT_COMMANDS_QUEUE, queue =>
                queue.BindExchange(EventRouting.COMMANDS_EXCHANGE, EventRouting.RoutingKeys.GRANT_REWARD));
        }

        private void ConfigureMatchmakingCommandListeners()
        {
            options.ListenToRabbitQueue(MATCHMAKING_COMMANDS_QUEUE, queue =>
                queue.BindExchange(
                    MatchmakingEventRouting.COMMANDS_EXCHANGE,
                    MatchmakingEventRouting.RoutingKeys.GRANT_REWARD));
        }
    }
}
