namespace WalletService.Contracts.Requests;

public sealed record TopUpWalletRequest(
    string Currency,
    long Amount,
    string Reason,
    Guid? OperationId,
    string? ReferenceId);
