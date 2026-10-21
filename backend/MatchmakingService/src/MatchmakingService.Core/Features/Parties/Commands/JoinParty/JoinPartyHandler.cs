using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Parties;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Parties.Commands.JoinParty;

public sealed class JoinPartyHandler(
    IPartyRepository partyRepository,
    ITransactionManager transactionManager) : ICommandHandler<PartyDto, JoinPartyCommand>
{
    public async Task<Result<PartyDto, Error>> Handle(
        JoinPartyCommand command,
        CancellationToken cancellationToken = default)
    {
        string code = (command.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
        {
            return MatchmakingErrors.PartyCodeNotFound;
        }

        Party? current = await partyRepository.GetByMemberAsync(command.AccountId, cancellationToken);
        if (current is not null)
        {
            return MatchmakingErrors.PartyAlreadyMember;
        }

        Party? party = await partyRepository.GetByCodeAsync(code, cancellationToken);
        if (party is null)
        {
            return MatchmakingErrors.PartyCodeNotFound;
        }

        var joinResult = party.Join(command.AccountId, DateTime.UtcNow);
        if (joinResult.IsFailure)
        {
            return joinResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : party.ToDto();
    }
}
