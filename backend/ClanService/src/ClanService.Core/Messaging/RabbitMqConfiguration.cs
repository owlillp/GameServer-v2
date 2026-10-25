using Shared.IntegrationEvents.ClanWars;
using Shared.IntegrationEvents.Clans;
using Shared.IntegrationEvents.Clans.Events;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace ClanService.Core.Messaging;

public static class RabbitMqConfiguration
{
    private const string CLANWARS_EVENTS_QUEUE = "clan.clanwars-events";

    extension(WolverineOptions options)
    {
        public void ConfigureRabbitMq(string connectionString)
        {
            options.UseRabbitMq(new Uri(connectionString))
                .AutoProvision()
                .EnableWolverineControlQueues()
                .UseQuorumQueues()
                .UsePlatformChannelDefaults()
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

            options.ConfigureEventPublishing();

            options.ListenToRabbitQueue(CLANWARS_EVENTS_QUEUE, queue =>
                queue.BindExchange(
                    ClanWarsEventRouting.EVENTS_EXCHANGE,
                    ClanWarsEventRouting.RoutingKeys.CLAN_POINTS_GRANTED));
        }

        private void ConfigureEventPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<ClanCreatedEvent>(
                    ClanEventRouting.EVENTS_EXCHANGE,
                    _ => ClanEventRouting.RoutingKeys.CLAN_CREATED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<ClanDisbandedEvent>(
                    ClanEventRouting.EVENTS_EXCHANGE,
                    _ => ClanEventRouting.RoutingKeys.CLAN_DISBANDED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<ClanMemberJoinedEvent>(
                    ClanEventRouting.EVENTS_EXCHANGE,
                    _ => ClanEventRouting.RoutingKeys.MEMBER_JOINED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<ClanMemberLeftEvent>(
                    ClanEventRouting.EVENTS_EXCHANGE,
                    _ => ClanEventRouting.RoutingKeys.MEMBER_LEFT)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<ClanMemberRoleChangedEvent>(
                    ClanEventRouting.EVENTS_EXCHANGE,
                    _ => ClanEventRouting.RoutingKeys.MEMBER_ROLE_CHANGED)
                .UseDurableOutbox();
        }
    }
}
