namespace WalletService.Contracts.Dtos;

public sealed record LedgerEntryDto(
    Guid Id,
    string Currency,
    string Direction,
    long Amount,
    string Reason,
    Guid OperationId,
    string? ReferenceId,
    string ActorType,
    Guid? ActorId,
    string? Comment,
    DateTime CreatedAt);
