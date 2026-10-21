using CSharpFunctionalExtensions;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Queues;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Parties.Commands.LeaveParty;

public sealed class LeavePartyHandler(
    IPartyRepository partyRepository,
    IQueueRepository queueRepository,
    ITransactionManager transactionManager) : ICommandHandler<bool, LeavePartyCommand>
{
    public async Task<Result<bool, Error>> Handle(
        LeavePartyCommand command,
        CancellationToken cancellationToken = default)
    {
        Party? party = await partyRepository.GetByMemberAsync(command.AccountId, cancellationToken);
        if (party is null)
        {
            return MatchmakingErrors.PartyNotFound;
        }

        DateTime now = DateTime.UtcNow;

        if (party.Status == PartyStatus.Queued)
        {
            QueueTicket? ticket = await queueRepository.GetActiveByPartyAsync(party.Id, cancellationToken);
            ticket?.Cancel(now);
            party.ReleaseFromQueue(now);
        }

        var leaveResult = party.Leave(command.AccountId, now);
        if (leaveResult.IsFailure)
        {
            return leaveResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return Result.Success<bool, Error>(true);
    }
}
