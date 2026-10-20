using System.Globalization;
using CSharpFunctionalExtensions;
using EventService.Contracts.Dtos;
using EventService.Core.Abstractions;
using EventService.Core.Features.Events.Models;
using EventService.Domain.Events;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Events.Queries.GetEvents;

public sealed class GetEventsHandler(IEventRepository eventRepository)
    : IQueryHandler<IReadOnlyList<EventDto>, GetEventsQuery>
{
    public async Task<Result<IReadOnlyList<EventDto>, Error>> Handle(
        GetEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        EventPhase? phase = null;
        if (!string.IsNullOrWhiteSpace(query.Phase))
        {
            if (int.TryParse(query.Phase, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                || !Enum.TryParse(query.Phase, ignoreCase: true, out EventPhase parsed)
                || !Enum.IsDefined(parsed))
            {
                return GeneralErrors.ValueIsInvalid("phase");
            }

            phase = parsed;
        }

        IReadOnlyList<GameEvent> events = await eventRepository.ListAsync(
            includeDetails: true,
            cancellationToken);

        DateTime now = DateTime.UtcNow;

        return events
            .Where(gameEvent => query.IncludeUnpublished || gameEvent.Status == EventStatus.PUBLISHED)
            .Where(gameEvent => phase is null || gameEvent.GetPhase(now) == phase)
            .OrderBy(gameEvent => gameEvent.StartsAt)
            .ThenBy(gameEvent => gameEvent.DisplayName, StringComparer.Ordinal)
            .Select(gameEvent => EventMapper.ToDto(gameEvent, now))
            .ToList();
    }
}
