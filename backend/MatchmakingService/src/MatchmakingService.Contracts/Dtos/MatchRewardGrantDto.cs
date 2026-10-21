namespace MatchmakingService.Contracts.Dtos;

public sealed record MatchRewardGrantDto(
    Guid Id,
    Guid AccountId,
    long Amount,
    string Status,
    string? FailureReason,
    DateTime? WalletCreditedAt,
    DateTime? ItemsGrantedAt);
