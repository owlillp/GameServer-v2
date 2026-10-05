namespace Shared.IntegrationEvents.Wallets.Events;

public record WalletDebitedEvent(
    Guid WalletId,
    string Currency,
    long Amount,
    string Reason,
    Guid OperationId,
    DateTime OccurredAt);
