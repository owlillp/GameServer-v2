namespace ClanService.Contracts.Dtos;

public sealed record ClanDto(
    Guid Id,
    string Tag,
    string Name,
    string? Description,
    Guid LeaderAccountId,
    int MemberCount,
    int MaxMembers,
    long ClanPoints,
    string Status,
    DateTime CreatedAt);

public sealed record ClanMemberDto(Guid AccountId, string Role, DateTime JoinedAt);

public sealed record ClanDetailsDto(ClanDto Clan, IReadOnlyList<ClanMemberDto> Members);

public sealed record MyClanDto(ClanDto Clan, string MyRole, IReadOnlyList<ClanMemberDto> Members);

public sealed record ClanJoinRequestDto(
    Guid Id,
    Guid ClanId,
    string ClanTag,
    Guid AccountId,
    string Kind,
    string Status,
    Guid CreatedBy,
    DateTime CreatedAt);

public sealed record ClanPointsEntryDto(
    Guid Id,
    Guid OperationId,
    long Amount,
    string Reason,
    Guid? ActorId,
    string? Comment,
    DateTime CreatedAt);
