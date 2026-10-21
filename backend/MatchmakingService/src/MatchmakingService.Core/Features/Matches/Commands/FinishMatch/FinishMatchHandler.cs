using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Core.Services;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Rewards;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Matches.Commands.FinishMatch;

public sealed class FinishMatchHandler(
    IMatchRepository matchRepository,
    IMatchRewardRepository rewardRepository,
    MatchFinalizer finalizer) : ICommandHandler<AdminMatchDto, FinishMatchCommand>
{
    public async Task<Result<AdminMatchDto, Error>> Handle(
        FinishMatchCommand command,
        CancellationToken cancellationToken = default)
    {
        Match? match = await matchRepository.GetAsync(command.MatchId, cancellationToken);
        if (match is null)
        {
            return MatchmakingErrors.MatchNotFound;
        }

        var finishResult = await finalizer.FinishAsync(match, cancellationToken);
        if (finishResult.IsFailure)
        {
            return finishResult.Error;
        }

        IReadOnlyList<MatchRewardGrant> grants = await rewardRepository.ListByMatchAsync(
            match.Id,
            cancellationToken);

        return match.ToAdminDto(grants);
    }
}
