namespace MatchmakingService.Contracts.Dtos;

public sealed record QueueTicketDto(
    Guid Id,
    Guid PartyId,
    string Mode,
    string Status,
    int PartySize,
    DateTime EnqueuedAt,
    Guid? MatchId,
    string? Region = null);
