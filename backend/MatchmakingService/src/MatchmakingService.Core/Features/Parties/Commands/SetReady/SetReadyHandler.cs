using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Parties;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Parties.Commands.SetReady;

public sealed class SetReadyHandler(
    IPartyRepository partyRepository,
    ITransactionManager transactionManager) : ICommandHandler<PartyDto, SetReadyCommand>
{
    public async Task<Result<PartyDto, Error>> Handle(
        SetReadyCommand command,
        CancellationToken cancellationToken = default)
    {
        Party? party = await partyRepository.GetByMemberAsync(command.AccountId, cancellationToken);
        if (party is null)
        {
            return MatchmakingErrors.PartyNotFound;
        }

        var readyResult = party.SetReady(command.AccountId, command.IsReady, DateTime.UtcNow);
        if (readyResult.IsFailure)
        {
            return readyResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : party.ToDto();
    }
}
