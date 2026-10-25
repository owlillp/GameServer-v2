using Shared.IntegrationEvents.Matchmaking;
using Shared.IntegrationEvents.Matchmaking.Commands;
using Shared.IntegrationEvents.Sessions;
using Shared.IntegrationEvents.Sessions.Events;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace SessionService.Core.Messaging;

public static class RabbitMqConfiguration
{
    private const string MATCHMAKING_COMMANDS_QUEUE = "session.matchmaking-commands";

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
                })
                .DeclareExchange(MatchmakingEventRouting.COMMANDS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                });

            options.ConfigureEventPublishing();
            options.ConfigureCommandListeners();
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

            options.PublishMessagesToRabbitMqExchange<MatchAllocatedEvent>(
                    SessionEventRouting.EVENTS_EXCHANGE,
                    _ => SessionEventRouting.RoutingKeys.MATCH_ALLOCATED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<MatchAllocationFailedEvent>(
                    SessionEventRouting.EVENTS_EXCHANGE,
                    _ => SessionEventRouting.RoutingKeys.MATCH_ALLOCATION_FAILED)
                .UseDurableOutbox();
        }

        private void ConfigureCommandListeners()
        {
            options.ListenToRabbitQueue(MATCHMAKING_COMMANDS_QUEUE, queue =>
                queue.BindExchange(
                    MatchmakingEventRouting.COMMANDS_EXCHANGE,
                    MatchmakingEventRouting.RoutingKeys.ALLOCATE_MATCH));
        }
    }
}
