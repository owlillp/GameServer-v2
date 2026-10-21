using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Parties;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Parties.Commands.KickMember;

public sealed class KickMemberHandler(
    IPartyRepository partyRepository,
    ITransactionManager transactionManager) : ICommandHandler<PartyDto, KickMemberCommand>
{
    public async Task<Result<PartyDto, Error>> Handle(
        KickMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        Party? party = await partyRepository.GetByMemberAsync(command.LeaderAccountId, cancellationToken);
        if (party is null)
        {
            return MatchmakingErrors.PartyNotFound;
        }

        var kickResult = party.Kick(command.LeaderAccountId, command.AccountId, DateTime.UtcNow);
        if (kickResult.IsFailure)
        {
            return kickResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : party.ToDto();
    }
}
