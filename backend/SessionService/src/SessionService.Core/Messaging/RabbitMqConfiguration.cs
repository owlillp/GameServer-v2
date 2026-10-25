using Shared.IntegrationEvents.Sessions;
using Shared.IntegrationEvents.Sessions.Events;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace SessionService.Core.Messaging;

public static class RabbitMqConfiguration
{
    extension(WolverineOptions options)
    {
        public void ConfigureRabbitMq(string connectionString)
        {
            options.UseRabbitMq(new Uri(connectionString))
                .AutoProvision()
                .EnableWolverineControlQueues()
                .UseQuorumQueues()
                .UsePlatformChannelDefaults()
                .DeclareExchange(SessionEventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                });

            options.ConfigureEventPublishing();
        }

        private void ConfigureEventPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<MatchCompletedEvent>(
                    SessionEventRouting.EVENTS_EXCHANGE,
                    _ => SessionEventRouting.RoutingKeys.MATCH_COMPLETED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<PlayerJoinedSessionEvent>(
                    SessionEventRouting.EVENTS_EXCHANGE,
                    _ => SessionEventRouting.RoutingKeys.PLAYER_JOINED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<PlayerLeftSessionEvent>(
                    SessionEventRouting.EVENTS_EXCHANGE,
                    _ => SessionEventRouting.RoutingKeys.PLAYER_LEFT)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<SessionClosedEvent>(
                    SessionEventRouting.EVENTS_EXCHANGE,
                    _ => SessionEventRouting.RoutingKeys.SESSION_CLOSED)
                .UseDurableOutbox();
        }
    }
}
