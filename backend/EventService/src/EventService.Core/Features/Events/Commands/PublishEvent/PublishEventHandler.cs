using CSharpFunctionalExtensions;
using EventService.Contracts.Dtos;
using EventService.Core.Abstractions;
using EventService.Core.Features.Events.Models;
using EventService.Core.Scheduling;
using Microsoft.Extensions.Logging;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Events.Commands.PublishEvent;

public sealed class PublishEventHandler(
    IEventRepository eventRepository,
    IEventScheduler eventScheduler,
    ITransactionManager transactionManager,
    ILogger<PublishEventHandler> logger) : ICommandHandler<EventDto, PublishEventCommand>
{
    public async Task<Result<EventDto, Error>> Handle(
        PublishEventCommand command,
        CancellationToken cancellationToken = default)
    {
        var eventResult = await eventRepository.GetByIdAsync(
            command.EventId,
            includeDetails: true,
            cancellationToken);

        if (eventResult.IsFailure)
        {
            return eventResult.Error;
        }

        var publishResult = eventResult.Value.Publish();
        if (publishResult.IsFailure)
        {
            return publishResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        try
        {
            await eventScheduler.ScheduleAsync(eventResult.Value, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Event {EventCode} was published but its scheduler triggers were not created",
                eventResult.Value.Code);
        }

        return EventMapper.ToDto(eventResult.Value, DateTime.UtcNow);
    }
}
