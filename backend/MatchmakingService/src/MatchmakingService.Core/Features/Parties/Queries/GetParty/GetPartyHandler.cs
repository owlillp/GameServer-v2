using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Parties;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Parties.Queries.GetParty;

public sealed class GetPartyHandler(IPartyRepository partyRepository)
    : IQueryHandler<PartyDto, GetPartyQuery>
{
    public async Task<Result<PartyDto, Error>> Handle(
        GetPartyQuery query,
        CancellationToken cancellationToken = default)
    {
        Party? party = query.PartyId is not null
            ? await partyRepository.GetAsync(query.PartyId.Value, cancellationToken)
            : await partyRepository.GetByCodeAsync(query.Code ?? string.Empty, cancellationToken);

        return party is null
            ? MatchmakingErrors.PartyNotFound
            : party.ToDto();
    }
}
