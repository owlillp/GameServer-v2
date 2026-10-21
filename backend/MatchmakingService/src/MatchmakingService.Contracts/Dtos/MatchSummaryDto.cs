namespace MatchmakingService.Contracts.Dtos;

public sealed record MatchSummaryDto(
    Guid Id,
    string Mode,
    string Status,
    DateTime StartsAt,
    DateTime EndsAt,
    DateTime? FinishedAt,
    Guid? WinnerPartyId);
