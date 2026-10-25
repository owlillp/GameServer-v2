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

public sealed record CreateSeasonRequest(
    string Name,
    DateTime StartsAt,
    DateTime EndsAt,
    int? FinalizationGraceMinutes,
    ScoringRulesDto? ScoringRules,
    Guid? RewardTableId);

public sealed record AdjustScoreRequest(Guid ClanId, long Amount, string Reason, Guid? OperationId);
