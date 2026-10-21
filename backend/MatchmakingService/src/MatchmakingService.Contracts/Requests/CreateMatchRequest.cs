namespace MatchmakingService.Contracts.Requests;

public sealed record CreateMatchRequest(
    Guid PartyAId,
    Guid PartyBId,
    string? Mode,
    int? DurationHours,
    MatchRewardRequest? Reward);
