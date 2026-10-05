namespace WalletService.Contracts.Requests;

public sealed record CreditWalletRequest(
    string Currency,
    long Amount,
    string Reason,
    Guid? OperationId,
    string? ReferenceId);
