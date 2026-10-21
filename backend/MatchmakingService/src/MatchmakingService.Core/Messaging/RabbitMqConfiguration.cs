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
        }

        private void ConfigureEventPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<MatchCompletedEvent>(
                    SessionEventRouting.EVENTS_EXCHANGE,
                    _ => SessionEventRouting.RoutingKeys.MATCH_COMPLETED)
                .UseDurableOutbox();
        }

        private void ConfigureEventListeners()
        {
            options.ListenToRabbitQueue(REWARD_EVENTS_QUEUE, queue =>
            {
                queue.BindExchange(MatchmakingEventRouting.EVENTS_EXCHANGE, MatchmakingEventRouting.RoutingKeys.REWARD_GRANTED);
                queue.BindExchange(MatchmakingEventRouting.EVENTS_EXCHANGE, MatchmakingEventRouting.RoutingKeys.REWARD_FAILED);
            });
        }
    }
}
