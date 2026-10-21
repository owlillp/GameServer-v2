using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain.Matches;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Matches.Queries.GetMyMatch;

public sealed class GetMyMatchHandler(IMatchRepository matchRepository)
    : IQueryHandler<MatchDetailsDto?, GetMyMatchQuery>
{
    public async Task<Result<MatchDetailsDto?, Error>> Handle(
        GetMyMatchQuery query,
        CancellationToken cancellationToken = default)
    {
        Match? match = await matchRepository.GetActiveByAccountAsync(query.AccountId, cancellationToken);

        return match?.ToDetailsDto(query.AccountId);
    }
}
