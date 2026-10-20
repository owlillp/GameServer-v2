using CSharpFunctionalExtensions;
using EventService.Contracts.Dtos;
using EventService.Core.Abstractions;
using EventService.Core.Features.Events.Models;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Events.Commands.PublishEvent;

public sealed class PublishEventHandler(
    IEventRepository eventRepository,
    ITransactionManager transactionManager) : ICommandHandler<EventDto, PublishEventCommand>
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
        return saveResult.IsFailure
            ? saveResult.Error
            : EventMapper.ToDto(eventResult.Value, DateTime.UtcNow);
    }
}
