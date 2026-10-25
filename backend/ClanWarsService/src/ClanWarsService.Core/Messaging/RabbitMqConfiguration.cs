using Shared.IntegrationEvents.ClanWars;
using Shared.IntegrationEvents.ClanWars.Events;
using Shared.IntegrationEvents.Clans;
using Shared.IntegrationEvents.Matchmaking;
using Shared.IntegrationEvents.Matchmaking.Commands;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace ClanWarsService.Core.Messaging;

public static class RabbitMqConfiguration
{
    private const string CLAN_EVENTS_QUEUE = "clanwars.clan-events";
    private const string MATCHMAKING_EVENTS_QUEUE = "clanwars.matchmaking-events";

    extension(WolverineOptions options)
    {
        public void ConfigureRabbitMq(string connectionString)
        {
            options.UseRabbitMq(new Uri(connectionString))
                .AutoProvision()
                .EnableWolverineControlQueues()
                .UseQuorumQueues()
                .UsePlatformChannelDefaults()
                .DeclareExchange(ClanWarsEventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(ClanEventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(MatchmakingEventRouting.COMMANDS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(MatchmakingEventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                });

            options.PublishMessagesToRabbitMqExchange<SeasonStartedEvent>(
                    ClanWarsEventRouting.EVENTS_EXCHANGE,
                    _ => ClanWarsEventRouting.RoutingKeys.SEASON_STARTED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<SeasonEndedEvent>(
                    ClanWarsEventRouting.EVENTS_EXCHANGE,
                    _ => ClanWarsEventRouting.RoutingKeys.SEASON_ENDED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<BattleFoundEvent>(
                    ClanWarsEventRouting.EVENTS_EXCHANGE,
                    _ => ClanWarsEventRouting.RoutingKeys.BATTLE_FOUND)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<BattleFinishedEvent>(
                    ClanWarsEventRouting.EVENTS_EXCHANGE,
                    _ => ClanWarsEventRouting.RoutingKeys.BATTLE_FINISHED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<CreateExternalMatchCommand>(
                    MatchmakingEventRouting.COMMANDS_EXCHANGE,
                    _ => MatchmakingEventRouting.RoutingKeys.CREATE_EXTERNAL_MATCH)
                .UseDurableOutbox();

            options.ListenToRabbitQueue(MATCHMAKING_EVENTS_QUEUE, queue =>
            {
                queue.BindExchange(MatchmakingEventRouting.EVENTS_EXCHANGE, MatchmakingEventRouting.RoutingKeys.EXTERNAL_MATCH_STARTED);
                queue.BindExchange(MatchmakingEventRouting.EVENTS_EXCHANGE, MatchmakingEventRouting.RoutingKeys.EXTERNAL_MATCH_COMPLETED);
                queue.BindExchange(MatchmakingEventRouting.EVENTS_EXCHANGE, MatchmakingEventRouting.RoutingKeys.EXTERNAL_MATCH_CANCELLED);
            });

            options.ListenToRabbitQueue(CLAN_EVENTS_QUEUE, queue =>
                queue.BindExchange(ClanEventRouting.EVENTS_EXCHANGE, ClanEventRouting.RoutingKeys.ALL_CLAN_EVENTS));
        }
    }
}
