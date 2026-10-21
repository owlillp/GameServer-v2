namespace MatchmakingService.Contracts.Dtos;

public sealed record MatchDetailsDto(
    Guid Id,
    string Mode,
    string Status,
    DateTime StartsAt,
    DateTime EndsAt,
    DateTime? FinishedAt,
    Guid? WinnerPartyId,
    Guid? MyPartyId,
    IReadOnlyList<MatchPartyDto> Parties,
    IReadOnlyList<MatchParticipantDto> Participants,
    IReadOnlyList<MatchResultDto> Results,
    MatchRewardDto? Reward);
