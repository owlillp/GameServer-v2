using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Matches.Commands.CancelMatch;

public sealed class CancelMatchHandler(
    IMatchRepository matchRepository,
    IPartyRepository partyRepository,
    IMatchmakingEventPublisher eventPublisher,
    ITransactionManager transactionManager) : ICommandHandler<MatchDetailsDto, CancelMatchCommand>
{
    public async Task<Result<MatchDetailsDto, Error>> Handle(
        CancelMatchCommand command,
        CancellationToken cancellationToken = default)
    {
        Match? match = await matchRepository.GetAsync(command.MatchId, cancellationToken);
        if (match is null)
        {
            return MatchmakingErrors.MatchNotFound;
        }

        DateTime now = DateTime.UtcNow;
        var cancelResult = match.Cancel(now);
        if (cancelResult.IsFailure)
        {
            return cancelResult.Error;
        }

        if (match.IsExternal)
        {
            await eventPublisher.PublishExternalCancelledAsync(match, "admin", cancellationToken);
        }

        var partyIds = match.Parties.Select(party => party.PartyId).ToArray();
        IReadOnlyList<Party> parties = await partyRepository.ListAsync(partyIds, cancellationToken);

        foreach (Party party in parties)
        {
            party.ReleaseFromMatch(now);
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : match.ToDetailsDto();
    }
}
