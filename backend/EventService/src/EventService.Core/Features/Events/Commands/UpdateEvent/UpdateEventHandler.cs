using CSharpFunctionalExtensions;
using FluentValidation;
using EventService.Contracts.Dtos;
using EventService.Core.Abstractions;
using EventService.Core.Features.Events.Models;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Events.Commands.UpdateEvent;

public sealed class UpdateEventHandler(
    IValidator<UpdateEventCommand> validator,
    IEventRepository eventRepository,
    ITransactionManager transactionManager) : ICommandHandler<EventDto, UpdateEventCommand>
{
    public async Task<Result<EventDto, Error>> Handle(
        UpdateEventCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var eventResult = await eventRepository.GetByIdAsync(
            command.EventId,
            includeDetails: true,
            cancellationToken);

        if (eventResult.IsFailure)
        {
            return eventResult.Error;
        }

        var kindResult = EventRequestMapper.ParseKind(command.Request.Kind);
        if (kindResult.IsFailure)
        {
            return kindResult.Error;
        }

        var questsResult = EventRequestMapper.MapQuests(command.Request.Quests);
        if (questsResult.IsFailure)
        {
            return questsResult.Error;
        }

        var updateResult = eventResult.Value.Update(
            command.Request.DisplayName,
            command.Request.Description,
            kindResult.Value,
            command.Request.CurrencyCode,
            command.Request.AnnounceAt,
            command.Request.StartsAt,
            command.Request.EndsAt,
            command.Request.GraceEndsAt,
            questsResult.Value,
            EventRequestMapper.MapRewards(command.Request.Rewards));

        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure
            ? saveResult.Error
            : EventMapper.ToDto(eventResult.Value, DateTime.UtcNow);
    }
}
