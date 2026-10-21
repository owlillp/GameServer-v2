namespace MatchmakingService.Domain.Rewards;

public sealed record MatchRewardData(
    string CurrencyCode,
    long ParticipationAmount,
    long WinBonusAmount,
    IReadOnlyList<MatchRewardItemData> Items);
