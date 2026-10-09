using Shared.IntegrationEvents.Catalog;
using Shared.IntegrationEvents.Catalog.Events;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace CatalogService.Core.Messaging;

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
                .DeclareExchange(CatalogEventRouting.EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                });

            options.ConfigureCatalogEventPublishing();
        }

        private void ConfigureCatalogEventPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<CatalogVersionPublishedEvent>(
                    CatalogEventRouting.EXCHANGE,
                    _ => CatalogEventRouting.RoutingKeys.VERSION_PUBLISHED)
                .UseDurableOutbox();
        }
    }
}
