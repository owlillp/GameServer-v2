namespace MatchmakingService.Contracts.Dtos;

public sealed record PartyMemberDto(
    Guid AccountId,
    bool IsReady,
    DateTime JoinedAt);
