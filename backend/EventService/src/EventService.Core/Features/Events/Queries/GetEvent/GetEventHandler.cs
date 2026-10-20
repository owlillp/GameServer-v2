using CSharpFunctionalExtensions;
using EventService.Contracts.Dtos;
using EventService.Core.Abstractions;
using EventService.Core.Features.Events.Models;
using Shared.Core.Abstractions;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Events.Queries.GetEvent;

public sealed class GetEventHandler(IEventRepository eventRepository)
    : IQueryHandler<EventDto, GetEventQuery>
{
    public async Task<Result<EventDto, Error>> Handle(
        GetEventQuery query,
        CancellationToken cancellationToken = default)
    {
        var eventResult = await eventRepository.GetByIdAsync(
            query.EventId,
            includeDetails: true,
            cancellationToken);

        return eventResult.IsFailure
            ? eventResult.Error
            : EventMapper.ToDto(eventResult.Value, DateTime.UtcNow);
    }
}
