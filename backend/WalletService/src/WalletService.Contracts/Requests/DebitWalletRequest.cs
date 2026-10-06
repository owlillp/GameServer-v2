namespace WalletService.Contracts.Requests;

public sealed record DebitWalletRequest(
    string Currency,
    long Amount,
    string Reason,
    Guid? OperationId,
    string? ReferenceId);
