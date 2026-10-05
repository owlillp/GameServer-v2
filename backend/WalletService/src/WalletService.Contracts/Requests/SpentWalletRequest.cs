namespace WalletService.Contracts.Requests;

public sealed record SpentWalletRequest(
    string Currency,
    long Amount,
    string Reason,
    Guid? OperationId,
    string? ReferenceId);
