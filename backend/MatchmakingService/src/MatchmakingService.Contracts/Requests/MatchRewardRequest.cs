namespace MatchmakingService.Contracts.Requests;

public sealed record MatchRewardRequest(
    string CurrencyCode,
    long ParticipationAmount,
    long WinBonusAmount,
    IReadOnlyList<MatchRewardItemRequest> Items);
