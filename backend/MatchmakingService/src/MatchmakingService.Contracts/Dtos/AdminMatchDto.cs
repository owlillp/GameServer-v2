namespace MatchmakingService.Contracts.Dtos;

public sealed record AdminMatchDto(
    MatchDetailsDto Match,
    IReadOnlyList<MatchRewardGrantDto> Grants);
