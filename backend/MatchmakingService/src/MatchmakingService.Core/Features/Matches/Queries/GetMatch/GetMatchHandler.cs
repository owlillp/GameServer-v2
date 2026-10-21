using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Matches;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Matches.Queries.GetMatch;

public sealed class GetMatchHandler(IMatchRepository matchRepository)
    : IQueryHandler<MatchDetailsDto, GetMatchQuery>
{
    public async Task<Result<MatchDetailsDto, Error>> Handle(
        GetMatchQuery query,
        CancellationToken cancellationToken = default)
    {
        Match? match = await matchRepository.GetAsync(query.MatchId, cancellationToken);
        if (match is null)
        {
            return MatchmakingErrors.MatchNotFound;
        }

        if (!match.IsParticipant(query.AccountId))
        {
            return MatchmakingErrors.MatchParticipantNotFound;
        }

        return match.ToDetailsDto(query.AccountId);
    }
}
