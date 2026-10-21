using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Matches;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Matches.Commands.SubmitResults;

public sealed class SubmitResultsHandler(
    IMatchRepository matchRepository,
    ITransactionManager transactionManager) : ICommandHandler<MatchDetailsDto, SubmitResultsCommand>
{
    public async Task<Result<MatchDetailsDto, Error>> Handle(
        SubmitResultsCommand command,
        CancellationToken cancellationToken = default)
    {
        Match? match = await matchRepository.GetAsync(command.MatchId, cancellationToken);
        if (match is null)
        {
            return MatchmakingErrors.MatchNotFound;
        }

        var results = command.Results
            .Select(result => new MatchResultData(result.AccountId, result.Score, result.Kills))
            .ToList();

        var submitResult = match.SubmitResults(results, DateTime.UtcNow);
        if (submitResult.IsFailure)
        {
            return submitResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : match.ToDetailsDto();
    }
}
