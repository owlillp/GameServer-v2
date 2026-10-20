using CSharpFunctionalExtensions;
using EventService.Contracts.Dtos;
using EventService.Core.Abstractions;
using EventService.Core.Features.Events.Models;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Events.Commands.ArchiveEvent;

public sealed class ArchiveEventHandler(
    IEventRepository eventRepository,
    ITransactionManager transactionManager) : ICommandHandler<EventDto, ArchiveEventCommand>
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
        return saveResult.IsFailure
            ? saveResult.Error
            : EventMapper.ToDto(eventResult.Value, DateTime.UtcNow);
    }
}
