namespace Shared.IntegrationEvents.Realtime.Notifications;

public sealed record WalletChangedPayload(
    Guid AccountId,
    Guid WalletId,
    string Currency,
    long Amount,
    string Reason,
    Guid OperationId);
