namespace MatchmakingService.Contracts.Dtos;

public sealed record MatchRewardDto(
    string CurrencyCode,
    long ParticipationAmount,
    long WinBonusAmount,
    IReadOnlyList<MatchRewardItemDto> Items);
