using Shared.IntegrationEvents.Wallets.Events;
using WalletService.Domain.Wallets.Ledgers;

namespace WalletService.Core.Messaging.Publishers;

public interface IWalletEventPublisher
{
    Task PublishCreditedAsync(
        Guid walletId,
        string currency,
        long amount,
        LedgerReason reason,
        Guid operationId);

    Task PublishDebitedAsync(
        Guid walletId,
        string currency,
        long amount,
        LedgerReason reason,
        Guid operationId);
}

public sealed class WalletEventPublisher(IOutboxService outbox) : IWalletEventPublisher
{
    public Task PublishCreditedAsync(
        Guid walletId,
        string currency,
        long amount,
        LedgerReason reason,
        Guid operationId) =>
        outbox.PublishAsync(new WalletCreditedEvent(
            walletId,
            currency,
            amount,
            reason.ToString(),
            operationId,
            DateTime.UtcNow));

    public Task PublishDebitedAsync(
        Guid walletId,
        string currency,
        long amount,
        LedgerReason reason,
        Guid operationId) =>
        outbox.PublishAsync(new WalletDebitedEvent(
            walletId,
            currency,
            amount,
            reason.ToString(),
            operationId,
            DateTime.UtcNow));
}
