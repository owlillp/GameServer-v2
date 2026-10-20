using Shared.IntegrationEvents.Events;
using Shared.IntegrationEvents.Events.Commands;
using Shared.IntegrationEvents.Events.Events;
using Shared.IntegrationEvents.Sessions;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace EventService.Core.Messaging;

public static class RabbitMqConfiguration
{
    private const string REWARD_EVENTS_QUEUE = "event.reward-events";
    private const string SESSION_EVENTS_QUEUE = "event.session-events";

    extension(WolverineOptions options)
    {
        public void ConfigureRabbitMq(string connectionString)
        {
            options.UseRabbitMq(new Uri(connectionString))
                .AutoProvision()
                .EnableWolverineControlQueues()
                .UseQuorumQueues()
                .UsePlatformChannelDefaults()
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
            options.PublishMessagesToRabbitMqExchange<GrantEventRewardCommand>(
                    EventRouting.COMMANDS_EXCHANGE,
                    _ => EventRouting.RoutingKeys.GRANT_REWARD)
                .UseDurableOutbox();
        }

        private void ConfigureEventPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<EventStartedEvent>(
                    EventRouting.EVENTS_EXCHANGE,
                    _ => EventRouting.RoutingKeys.STARTED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<EventEndedEvent>(
                    EventRouting.EVENTS_EXCHANGE,
                    _ => EventRouting.RoutingKeys.ENDED)
                .UseDurableOutbox();
        }

        private void ConfigureEventListeners()
        {
            options.ListenToRabbitQueue(REWARD_EVENTS_QUEUE, queue =>
            {
                queue.BindExchange(EventRouting.EVENTS_EXCHANGE, EventRouting.RoutingKeys.REWARD_GRANTED);
                queue.BindExchange(EventRouting.EVENTS_EXCHANGE, EventRouting.RoutingKeys.REWARD_FAILED);
            });

            options.ListenToRabbitQueue(SESSION_EVENTS_QUEUE, queue =>
                queue.BindExchange(SessionEventRouting.EVENTS_EXCHANGE, SessionEventRouting.RoutingKeys.MATCH_COMPLETED));
        }
    }
}
