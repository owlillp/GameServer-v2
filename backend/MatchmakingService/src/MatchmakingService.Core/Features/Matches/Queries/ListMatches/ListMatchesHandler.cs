using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain.Matches;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Matches.Queries.ListMatches;

public sealed class ListMatchesHandler(IMatchRepository matchRepository)
    : IQueryHandler<IReadOnlyList<MatchDetailsDto>, ListMatchesQuery>
{
    public async Task<Result<IReadOnlyList<MatchDetailsDto>, Error>> Handle(
        ListMatchesQuery query,
        CancellationToken cancellationToken = default)
    {
        MatchStatus? status = Enum.TryParse(query.Status, true, out MatchStatus parsed)
            ? parsed
            : null;

        int limit = Math.Clamp(query.Limit, 1, 100);

        IReadOnlyList<Match> matches = await matchRepository.ListAsync(status, limit, cancellationToken);

        return matches
            .Select(match => match.ToDetailsDto())
            .ToList();
    }
}
