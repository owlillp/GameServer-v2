using CSharpFunctionalExtensions;
using EventService.Contracts.Dtos;
using EventService.Core.Abstractions;
using EventService.Core.Features.Events.Models;
using EventService.Domain;
using EventService.Domain.Events;
using EventService.Domain.Progress;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Events.Queries.GetEventDetails;

public sealed class GetEventDetailsHandler(
    IEventRepository eventRepository,
    IEventParticipationRepository participationRepository)
    : IQueryHandler<EventDetailsDto, GetEventDetailsQuery>
{
    public async Task<Result<EventDetailsDto, Error>> Handle(
        GetEventDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var eventResult = await eventRepository.GetByCodeAsync(
            query.EventCode,
            includeDetails: true,
            cancellationToken);

        if (eventResult.IsFailure)
        {
            return eventResult.Error;
        }

        GameEvent gameEvent = eventResult.Value;
        if (gameEvent.Status == EventStatus.DRAFT)
        {
            return EventErrors.EventNotFound;
        }

        EventProgress? progress = query.AccountId == Guid.Empty
            ? null
            : await participationRepository.FindProgressAsync(gameEvent.Id, query.AccountId, cancellationToken);

        IReadOnlyList<EventQuestProgress> questProgresses = progress is null
            ? []
            : await participationRepository.ListQuestProgressAsync(
                gameEvent.Id,
                query.AccountId,
                cancellationToken);

        IReadOnlyList<EventRewardClaim> claims = query.AccountId == Guid.Empty
            ? []
            : await participationRepository.ListClaimsAsync(
                query.AccountId,
                gameEvent.Id,
                cancellationToken);

        DateTime now = DateTime.UtcNow;

        return new EventDetailsDto(
            EventMapper.ToDto(gameEvent, now),
            EventMapper.ToProgressDto(gameEvent, progress, questProgresses, query.AccountId, now),
            [.. claims.OrderByDescending(claim => claim.CreatedAt).Select(EventMapper.ToClaimDto)]);
    }
}
