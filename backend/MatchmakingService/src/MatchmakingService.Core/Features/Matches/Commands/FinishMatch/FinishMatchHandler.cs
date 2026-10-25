using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Core.Services;
using MatchmakingService.Domain.Rewards;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Matches.Commands.FinishMatch;

public sealed class FinishMatchHandler(
    IMatchRewardRepository rewardRepository,
    MatchFinalizer finalizer) : ICommandHandler<AdminMatchDto, FinishMatchCommand>
{
    public async Task<Result<AdminMatchDto, Error>> Handle(
        FinishMatchCommand command,
        CancellationToken cancellationToken = default)
    {
        var finishResult = await finalizer.FinishAsync(command.MatchId, cancellationToken);
        if (finishResult.IsFailure)
        {
            return finishResult.Error;
        }

        IReadOnlyList<MatchRewardGrant> grants = await rewardRepository.ListByMatchAsync(
            command.MatchId,
            cancellationToken);

        return finishResult.Value.ToAdminDto(grants);
    }
}
