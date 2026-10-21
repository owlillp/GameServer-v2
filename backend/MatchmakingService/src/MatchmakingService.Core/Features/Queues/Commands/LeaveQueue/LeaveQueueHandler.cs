using CSharpFunctionalExtensions;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Queues;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Queues.Commands.LeaveQueue;

public sealed class LeaveQueueHandler(
    IPartyRepository partyRepository,
    IQueueRepository queueRepository,
    ITransactionManager transactionManager) : ICommandHandler<bool, LeaveQueueCommand>
{
    public async Task<Result<bool, Error>> Handle(
        LeaveQueueCommand command,
        CancellationToken cancellationToken = default)
    {
        Party? party = await partyRepository.GetByMemberAsync(command.AccountId, cancellationToken);
        if (party is null)
        {
            return MatchmakingErrors.PartyNotFound;
        }

        QueueTicket? ticket = await queueRepository.GetActiveByPartyAsync(party.Id, cancellationToken);
        if (ticket is null)
        {
            return MatchmakingErrors.PartyNotQueued;
        }

        DateTime now = DateTime.UtcNow;
        var cancelResult = ticket.Cancel(now);
        if (cancelResult.IsFailure)
        {
            return cancelResult.Error;
        }

        var releaseResult = party.ReleaseFromQueue(now);
        if (releaseResult.IsFailure)
        {
            return releaseResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return Result.Success<bool, Error>(true);
    }
}
