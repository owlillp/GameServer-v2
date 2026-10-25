using Shared.IntegrationEvents.ClanWars;
using Shared.IntegrationEvents.ClanWars.Events;
using Shared.IntegrationEvents.Clans;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace ClanWarsService.Core.Messaging;

public static class RabbitMqConfiguration
{
    private const string CLAN_EVENTS_QUEUE = "clanwars.clan-events";

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
                });

            options.PublishMessagesToRabbitMqExchange<SeasonStartedEvent>(
                    ClanWarsEventRouting.EVENTS_EXCHANGE,
                    _ => ClanWarsEventRouting.RoutingKeys.SEASON_STARTED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<SeasonEndedEvent>(
                    ClanWarsEventRouting.EVENTS_EXCHANGE,
                    _ => ClanWarsEventRouting.RoutingKeys.SEASON_ENDED)
                .UseDurableOutbox();

            options.ListenToRabbitQueue(CLAN_EVENTS_QUEUE, queue =>
                queue.BindExchange(ClanEventRouting.EVENTS_EXCHANGE, ClanEventRouting.RoutingKeys.ALL_CLAN_EVENTS));
        }
    }
}
