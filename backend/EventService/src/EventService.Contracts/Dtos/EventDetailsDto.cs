namespace EventService.Contracts.Dtos;

public sealed record EventDetailsDto(
    EventDto Event,
    EventProgressDto Progress,
    IReadOnlyList<EventRewardClaimDto> Claims);
