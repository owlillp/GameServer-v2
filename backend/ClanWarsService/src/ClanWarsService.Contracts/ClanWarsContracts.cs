namespace ClanWarsService.Contracts;

public sealed record ScoringRulesDto(int WinPoints, int DrawPoints, int LossPoints);

public sealed record SeasonDto(
    Guid Id,
    int Number,
    string Name,
    string Status,
    DateTime StartsAt,
    DateTime EndsAt,
    DateTime FinalizesAt,
    ScoringRulesDto ScoringRules,
    Guid? RewardTableId,
    DateTime? PublishedAt,
    DateTime? StartedAt,
    DateTime? EndedAt,
    DateTime? ClosedAt);

public sealed record LeaderboardRowDto(
    int Rank,
    Guid ClanId,
    string Tag,
    string Name,
    long Points,
    int Battles,
    int Wins);

/// <summary>
/// A clan's place with the clans just above and below it.
/// </summary>
public sealed record ClanStandingDto(Guid SeasonId, LeaderboardRowDto Clan, IReadOnlyList<LeaderboardRowDto> Neighbors);

public sealed record MemberContributionDto(Guid AccountId, long Points, int Battles, int Wins);

public sealed record ScoreReceiptDto(
    Guid Id,
    Guid SeasonId,
    Guid ClanId,
    string SourceId,
    long Amount,
    string Reason,
    Guid? ActorId,
    DateTime CreatedAt);

/// <param name="BattleWindowStartUtc">Daily battle window start, "HH:mm" UTC; with the end, e.g. "19:00"–"23:00".
/// Both empty: battles run all season long.</param>
public sealed record CreateSeasonRequest(
    string Name,
    DateTime StartsAt,
    DateTime EndsAt,
    int? FinalizationGraceMinutes,
    ScoringRulesDto? ScoringRules,
    Guid? RewardTableId,
    string? BattleWindowStartUtc = null,
    string? BattleWindowEndUtc = null);

public sealed record BattleWindowDto(DateTime StartsAt, DateTime EndsAt, bool IsOpen);

public sealed record QueueSquadRequest(IReadOnlyList<Guid> AccountIds);

public sealed record BattleQueueEntryDto(
    Guid Id,
    Guid SeasonId,
    Guid ClanId,
    IReadOnlyList<Guid> Squad,
    long Rating,
    string Status,
    Guid? BattleId,
    DateTime QueuedAt);

public sealed record BattleDto(
    Guid Id,
    Guid SeasonId,
    Guid ClanAId,
    Guid ClanBId,
    IReadOnlyList<Guid> SquadA,
    IReadOnlyList<Guid> SquadB,
    string Status,
    Guid? MatchId,
    Guid? WinnerClanId,
    long PointsA,
    long PointsB,
    string? CancelReason,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? FinishedAt);

public sealed record MyBattlesDto(BattleQueueEntryDto? Queued, IReadOnlyList<BattleDto> Battles);

public sealed record AdjustScoreRequest(Guid ClanId, long Amount, string Reason, Guid? OperationId);

public sealed record RewardItemDto(Guid ItemId, string Code, string Kind, string? Slot, int Quantity);

public sealed record RewardTierDto(
    int FromRank,
    int ToRank,
    long ClanPoints,
    string? CurrencyCode,
    long CurrencyAmount,
    IReadOnlyList<RewardItemDto> Items,
    int MinBattles);

public sealed record CreateRewardTableRequest(string Name, IReadOnlyList<RewardTierDto> Tiers);

public sealed record RewardTableDto(Guid Id, string Name, DateTime CreatedAt, IReadOnlyList<RewardTierDto> Tiers);

public sealed record RewardGrantDto(
    Guid Id,
    Guid SeasonId,
    Guid ClanId,
    Guid? AccountId,
    int Rank,
    long ClanPoints,
    string? CurrencyCode,
    long CurrencyAmount,
    IReadOnlyList<RewardItemDto> Items,
    string Status,
    int Attempts,
    string? LastError,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public sealed record SeasonRewardsDto(int Pending, int Granted, int Failed, IReadOnlyList<RewardGrantDto> Grants);
