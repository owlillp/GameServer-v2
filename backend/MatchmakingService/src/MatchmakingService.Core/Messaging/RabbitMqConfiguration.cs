using Shared.IntegrationEvents.Matchmaking;
using Shared.IntegrationEvents.Matchmaking.Commands;
using Shared.IntegrationEvents.Matchmaking.Events;
using Shared.IntegrationEvents.Sessions;
using Shared.IntegrationEvents.Sessions.Events;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace MatchmakingService.Core.Messaging;

public static class RabbitMqConfiguration
{
    private const string REWARD_EVENTS_QUEUE = "matchmaking.reward-events";
    private const string SESSION_EVENTS_QUEUE = "matchmaking.session-events";
    private const string EXTERNAL_COMMANDS_QUEUE = "matchmaking.external-commands";

    extension(WolverineOptions options)
    {
        public void ConfigureRabbitMq(string connectionString)
        {
            options.UseRabbitMq(new Uri(connectionString))
                .AutoProvision()
                .EnableWolverineControlQueues()
                .UseQuorumQueues()
                .UsePlatformChannelDefaults()
                .DeclareExchange(MatchmakingEventRouting.COMMANDS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(MatchmakingEventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(SessionEventRouting.EVENTS_EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                });

            options.ConfigureCommandPublishing();
            options.ConfigureEventPublishing();
            options.ConfigureEventListeners();
        }

        private void ConfigureCommandPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<GrantMatchRewardCommand>(
                    MatchmakingEventRouting.COMMANDS_EXCHANGE,
                    _ => MatchmakingEventRouting.RoutingKeys.GRANT_REWARD)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<AllocateMatchCommand>(
                    MatchmakingEventRouting.COMMANDS_EXCHANGE,
                    _ => MatchmakingEventRouting.RoutingKeys.ALLOCATE_MATCH)
                .UseDurableOutbox();
        }

        private void ConfigureEventPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<MatchCompletedEvent>(
                    SessionEventRouting.EVENTS_EXCHANGE,
                    _ => SessionEventRouting.RoutingKeys.MATCH_COMPLETED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<MatchReadyEvent>(
                    MatchmakingEventRouting.EVENTS_EXCHANGE,
                    _ => MatchmakingEventRouting.RoutingKeys.MATCH_READY)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<ExternalMatchStartedEvent>(
                    MatchmakingEventRouting.EVENTS_EXCHANGE,
                    _ => MatchmakingEventRouting.RoutingKeys.EXTERNAL_MATCH_STARTED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<ExternalMatchCompletedEvent>(
                    MatchmakingEventRouting.EVENTS_EXCHANGE,
                    _ => MatchmakingEventRouting.RoutingKeys.EXTERNAL_MATCH_COMPLETED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<ExternalMatchCancelledEvent>(
                    MatchmakingEventRouting.EVENTS_EXCHANGE,
                    _ => MatchmakingEventRouting.RoutingKeys.EXTERNAL_MATCH_CANCELLED)
                .UseDurableOutbox();
        }

        private void ConfigureEventListeners()
        {
            options.ListenToRabbitQueue(REWARD_EVENTS_QUEUE, queue =>
            {
                queue.BindExchange(MatchmakingEventRouting.EVENTS_EXCHANGE, MatchmakingEventRouting.RoutingKeys.REWARD_GRANTED);
                queue.BindExchange(MatchmakingEventRouting.EVENTS_EXCHANGE, MatchmakingEventRouting.RoutingKeys.REWARD_FAILED);
            });

            options.ListenToRabbitQueue(EXTERNAL_COMMANDS_QUEUE, queue =>
                queue.BindExchange(
                    MatchmakingEventRouting.COMMANDS_EXCHANGE,
                    MatchmakingEventRouting.RoutingKeys.CREATE_EXTERNAL_MATCH));

            options.ListenToRabbitQueue(SESSION_EVENTS_QUEUE, queue =>
            {
                queue.BindExchange(SessionEventRouting.EVENTS_EXCHANGE, SessionEventRouting.RoutingKeys.MATCH_COMPLETED);
                queue.BindExchange(SessionEventRouting.EVENTS_EXCHANGE, SessionEventRouting.RoutingKeys.MATCH_ALLOCATED);
                queue.BindExchange(SessionEventRouting.EVENTS_EXCHANGE, SessionEventRouting.RoutingKeys.MATCH_ALLOCATION_FAILED);
                queue.BindExchange(SessionEventRouting.EVENTS_EXCHANGE, SessionEventRouting.RoutingKeys.SESSION_CLOSED);
            });
        }
    }
}
