using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Queues;
using Microsoft.Extensions.Options;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Queues.Commands.EnqueueParty;

public sealed class EnqueuePartyHandler(
    IPartyRepository partyRepository,
    IQueueRepository queueRepository,
    IOptions<MatchmakingOptions> options,
    ITransactionManager transactionManager) : ICommandHandler<QueueTicketDto, EnqueuePartyCommand>
{
    public async Task<Result<QueueTicketDto, Error>> Handle(
        EnqueuePartyCommand command,
        CancellationToken cancellationToken = default)
    {
        Party? party = await partyRepository.GetByMemberAsync(command.AccountId, cancellationToken);
        if (party is null)
        {
            return MatchmakingErrors.PartyNotFound;
        }

        DateTime now = DateTime.UtcNow;
        var queuedResult = party.MarkQueued(command.AccountId, now);
        if (queuedResult.IsFailure)
        {
            return queuedResult.Error;
        }

        string mode = string.IsNullOrWhiteSpace(command.Mode)
            ? options.Value.DefaultMode
            : command.Mode;

        var ticketResult = QueueTicket.Create(party, mode, now);
        if (ticketResult.IsFailure)
        {
            return ticketResult.Error;
        }

        QueueTicket ticket = ticketResult.Value;
        await queueRepository.AddAsync(ticket, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : ticket.ToDto();
    }
}
