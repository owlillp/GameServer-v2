namespace MatchmakingService.Contracts.Dtos;

public sealed record PartyDto(
    Guid Id,
    string Code,
    Guid LeaderAccountId,
    string Status,
    int Size,
    DateTime CreatedAt,
    IReadOnlyList<PartyMemberDto> Members);
