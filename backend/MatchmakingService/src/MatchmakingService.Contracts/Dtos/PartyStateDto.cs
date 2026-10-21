namespace MatchmakingService.Contracts.Dtos;

public sealed record PartyStateDto(
    PartyDto? Party,
    QueueTicketDto? Ticket,
    MatchSummaryDto? ActiveMatch);
