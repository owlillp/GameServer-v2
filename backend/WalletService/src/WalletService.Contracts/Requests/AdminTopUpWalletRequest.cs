namespace WalletService.Contracts.Requests;

public sealed record AdminTopUpWalletRequest(
    string Currency,
    long Amount,
    string Reason,
    string Comment,
    Guid? OperationId,
    string? ReferenceId);
