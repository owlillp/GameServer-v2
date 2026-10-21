using CSharpFunctionalExtensions;
using MatchmakingService.Contracts.Dtos;
using MatchmakingService.Contracts.Requests;
using MatchmakingService.Core.Abstractions;
using MatchmakingService.Core.Configuration;
using MatchmakingService.Core.Features.Matchmaking.Models;
using MatchmakingService.Domain;
using MatchmakingService.Domain.Matches;
using MatchmakingService.Domain.Parties;
using MatchmakingService.Domain.Queues;
using MatchmakingService.Domain.Rewards;
using Microsoft.Extensions.Options;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace MatchmakingService.Core.Features.Matches.Commands.CreateMatch;

public sealed class CreateMatchHandler(
    IPartyRepository partyRepository,
    IQueueRepository queueRepository,
    IMatchRepository matchRepository,
    IOptions<MatchmakingOptions> options,
    ITransactionManager transactionManager) : ICommandHandler<MatchDetailsDto, CreateMatchCommand>
{
    public async Task<Result<MatchDetailsDto, Error>> Handle(
        CreateMatchCommand command,
        CancellationToken cancellationToken = default)
    {
        CreateMatchRequest request = command.Request;

        if (request.PartyAId == request.PartyBId || request.PartyAId == Guid.Empty || request.PartyBId == Guid.Empty)
        {
            return MatchmakingErrors.MatchPartiesInvalid;
        }

        Party? first = await partyRepository.GetAsync(request.PartyAId, cancellationToken);
        Party? second = await partyRepository.GetAsync(request.PartyBId, cancellationToken);

        if (first is null || second is null)
        {
            return MatchmakingErrors.PartyNotFound;
        }

        MatchmakingOptions current = options.Value;
        int durationHours = Math.Clamp(
            request.DurationHours ?? current.DefaultMatchDurationHours,
            1,
            MatchmakingConstants.MAX_MATCH_DURATION_HOURS);

        MatchRewardData reward = request.Reward is null
            ? new MatchRewardData(
                current.DefaultCurrencyCode,
                current.DefaultParticipationReward,
                current.DefaultWinBonus,
                [])
            : new MatchRewardData(
                request.Reward.CurrencyCode,
                request.Reward.ParticipationAmount,
                request.Reward.WinBonusAmount,
                [.. request.Reward.Items.Select(item => new MatchRewardItemData(
                    item.ItemId,
                    item.Code,
                    item.Kind,
                    item.Slot,
                    item.Quantity))]);

        DateTime startsAt = DateTime.UtcNow;
        DateTime endsAt = startsAt.AddHours(durationHours);

        var matchResult = Match.Create(
            first,
            second,
            request.Mode ?? current.DefaultMode,
            startsAt,
            endsAt,
            reward);

        if (matchResult.IsFailure)
        {
            return matchResult.Error;
        }

        Match match = matchResult.Value;

        var firstResult = first.MarkInMatch(startsAt);
        if (firstResult.IsFailure)
        {
            return firstResult.Error;
        }

        var secondResult = second.MarkInMatch(startsAt);
        if (secondResult.IsFailure)
        {
            return secondResult.Error;
        }

        await CancelTicketAsync(first.Id, cancellationToken);
        await CancelTicketAsync(second.Id, cancellationToken);

        await matchRepository.AddAsync(match, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error : match.ToDetailsDto();
    }

    private async Task CancelTicketAsync(Guid partyId, CancellationToken cancellationToken)
    {
        QueueTicket? ticket = await queueRepository.GetActiveByPartyAsync(partyId, cancellationToken);
        ticket?.Cancel(DateTime.UtcNow);
    }
}
