using Shared.IntegrationEvents.Identity;
using Shared.IntegrationEvents.Wallets;
using Shared.IntegrationEvents.Wallets.Events;
using Shared.Messaging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace WalletService.Core.Messaging;

public static class RabbitMqConfiguration
{
    private const string ACCOUNT_EVENTS_QUEUE = "wallet.account-events";

    extension(WolverineOptions options)
    {
        public void ConfigureRabbitMq(string connectionString)
        {
            options.UseRabbitMq(new Uri(connectionString))
                .AutoProvision()
                .EnableWolverineControlQueues()
                .UseQuorumQueues()
                .UsePlatformChannelDefaults()
                .DeclareExchange(IdentityEventRouting.EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                })
                .DeclareExchange(WalletEventRouting.EXCHANGE, exchange =>
                {
                    exchange.ExchangeType = ExchangeType.Topic;
                    exchange.IsDurable = true;
                });

            options.ConfigureWalletEventPublishing();
            options.ConfigureAccountEventsListeners();
        }

        private void ConfigureWalletEventPublishing()
        {
            options.PublishMessagesToRabbitMqExchange<WalletCreditedEvent>(
                    WalletEventRouting.EXCHANGE,
                    _ => WalletEventRouting.RoutingKeys.CREDITED)
                .UseDurableOutbox();

            options.PublishMessagesToRabbitMqExchange<WalletDebitedEvent>(
                    WalletEventRouting.EXCHANGE,
                    _ => WalletEventRouting.RoutingKeys.DEBITED)
                .UseDurableOutbox();
        }

        private void ConfigureAccountEventsListeners()
        {
            options.ListenToRabbitQueue(ACCOUNT_EVENTS_QUEUE, queue =>
                queue.BindExchange(IdentityEventRouting.EXCHANGE, IdentityEventRouting.RoutingKeys.ALL_ACCOUNT_EVENTS));
        }
    }
}
