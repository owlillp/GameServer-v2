using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Queues;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Parties.Queries.GetMyParty;

public sealed class GetMyPartyHandler(
    IPartyRepository partyRepository,
    IQueueRepository queueRepository,
    IMatchRepository matchRepository) : IQueryHandler<PartyStateDto, GetMyPartyQuery>
{
    public async Task<Result<PartyStateDto, Error>> Handle(
        GetMyPartyQuery query,
        CancellationToken cancellationToken = default)
    {
        Party? party = await partyRepository.GetByMemberAsync(query.AccountId, cancellationToken);

        if (party is null)
        {
            return new PartyStateDto(null, null, null);
        }

        QueueTicket? ticket = await queueRepository.GetActiveByPartyAsync(party.Id, cancellationToken);
        Match? activeMatch = party.Status == PartyStatus.InMatch
            ? await matchRepository.GetActiveByAccountAsync(query.AccountId, cancellationToken)
            : null;

        return new PartyStateDto(
            party.ToDto(),
            ticket?.ToDto(),
            activeMatch?.ToSummaryDto());
    }
}
