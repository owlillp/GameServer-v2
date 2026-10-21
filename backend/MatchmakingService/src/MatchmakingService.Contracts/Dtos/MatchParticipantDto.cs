namespace MatchmakingService.Contracts.Dtos;

public sealed record MatchParticipantDto(
    Guid AccountId,
    Guid PartyId);
