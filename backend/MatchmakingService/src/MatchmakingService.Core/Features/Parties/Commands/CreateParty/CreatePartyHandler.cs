using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Parties;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Parties.Commands.CreateParty;

public sealed class CreatePartyHandler(
    IPartyRepository partyRepository,
    ITransactionManager transactionManager) : ICommandHandler<PartyDto, CreatePartyCommand>
{
    public async Task<Result<PartyDto, Error>> Handle(
        CreatePartyCommand command,
        CancellationToken cancellationToken = default)
    {
        Party? existing = await partyRepository.GetByMemberAsync(command.AccountId, cancellationToken);
        if (existing is not null)
        {
            return MatchmakingErrors.PartyAlreadyMember;
        }

        var partyResult = Party.Create(command.AccountId, DateTime.UtcNow);
        if (partyResult.IsFailure)
        {
            return partyResult.Error;
        }

        Party party = partyResult.Value;
        await partyRepository.AddAsync(party, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : party.ToDto();
    }
}
