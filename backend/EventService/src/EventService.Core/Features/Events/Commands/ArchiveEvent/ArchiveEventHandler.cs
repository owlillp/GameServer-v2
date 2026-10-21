using CSharpFunctionalExtensions;
using EventService.Contracts.Dtos;
using EventService.Core.Abstractions;
using EventService.Core.Features.Events.Models;
using EventService.Core.Scheduling;
using Microsoft.Extensions.Logging;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Events.Commands.ArchiveEvent;

public sealed class ArchiveEventHandler(
    IEventRepository eventRepository,
    IEventScheduler eventScheduler,
    ITransactionManager transactionManager,
    ILogger<ArchiveEventHandler> logger) : ICommandHandler<EventDto, ArchiveEventCommand>
{
    public async Task<Result<EventDto, Error>> Handle(
        ArchiveEventCommand command,
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

        var archiveResult = eventResult.Value.Archive();
        if (archiveResult.IsFailure)
        {
            return archiveResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        try
        {
            await eventScheduler.UnscheduleAsync(command.EventId, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Event {EventId} was archived but its scheduler triggers were not removed",
                command.EventId);
        }

        return EventMapper.ToDto(eventResult.Value, DateTime.UtcNow);
    }
}
