namespace MatchmakingService.Contracts.Dtos;

public sealed record MatchPartyDto(
    Guid PartyId,
    int Side,
    long Score);
