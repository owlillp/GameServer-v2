using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Rewards;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Matches.Queries.GetAdminMatch;

public sealed class GetAdminMatchHandler(
    IMatchRepository matchRepository,
    IMatchRewardRepository rewardRepository) : IQueryHandler<AdminMatchDto, GetAdminMatchQuery>
{
    public async Task<Result<AdminMatchDto, Error>> Handle(
        GetAdminMatchQuery query,
        CancellationToken cancellationToken = default)
    {
        Match? match = await matchRepository.GetAsync(query.MatchId, cancellationToken);
        if (match is null)
        {
            return MatchmakingErrors.MatchNotFound;
        }

        IReadOnlyList<MatchRewardGrant> grants = await rewardRepository.ListByMatchAsync(
            match.Id,
            cancellationToken);

        return match.ToAdminDto(grants);
    }
}
