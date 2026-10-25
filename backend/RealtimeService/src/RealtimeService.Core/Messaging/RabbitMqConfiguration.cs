using Shared.IntegrationEvents.ClanWars;
using Shared.IntegrationEvents.Clans;
using Shared.IntegrationEvents.Events;
using Shared.IntegrationEvents.Inventories;
using Shared.IntegrationEvents.Matchmaking;
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
    private const string MATCHMAKING_EVENTS_QUEUE = "realtime.matchmaking-events";
    private const string CLAN_EVENTS_QUEUE = "realtime.clan-events";
    private const string CLANWARS_EVENTS_QUEUE = "realtime.clanwars-events";

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
                })
                .DeclareExchange(MatchmakingEventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(ClanEventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(ClanWarsEventRouting.EVENTS_EXCHANGE, exchange =>
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

            options.ListenToRabbitQueue(MATCHMAKING_EVENTS_QUEUE, queue =>
                queue.BindExchange(
                    MatchmakingEventRouting.EVENTS_EXCHANGE,
                    MatchmakingEventRouting.RoutingKeys.MATCH_READY));

            options.ListenToRabbitQueue(CLAN_EVENTS_QUEUE, queue =>
                queue.BindExchange(ClanEventRouting.EVENTS_EXCHANGE, ClanEventRouting.RoutingKeys.ALL_CLAN_EVENTS));

            options.ListenToRabbitQueue(CLANWARS_EVENTS_QUEUE, queue =>
            {
                queue.BindExchange(ClanWarsEventRouting.EVENTS_EXCHANGE, ClanWarsEventRouting.RoutingKeys.SEASON_STARTED);
                queue.BindExchange(ClanWarsEventRouting.EVENTS_EXCHANGE, ClanWarsEventRouting.RoutingKeys.SEASON_ENDED);
                queue.BindExchange(ClanWarsEventRouting.EVENTS_EXCHANGE, ClanWarsEventRouting.RoutingKeys.BATTLE_FOUND);
                queue.BindExchange(ClanWarsEventRouting.EVENTS_EXCHANGE, ClanWarsEventRouting.RoutingKeys.BATTLE_FINISHED);
            });
        }
    }
}
