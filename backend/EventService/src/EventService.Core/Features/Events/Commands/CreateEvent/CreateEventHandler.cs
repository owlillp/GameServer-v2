using CSharpFunctionalExtensions;
using FluentValidation;
using EventService.Contracts.Dtos;
using EventService.Core.Abstractions;
using EventService.Core.Features.Events.Models;
using EventService.Domain;
using EventService.Domain.Events;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Events.Commands.CreateEvent;

public sealed class CreateEventHandler(
    IValidator<CreateEventCommand> validator,
    IEventRepository eventRepository,
    ITransactionManager transactionManager) : ICommandHandler<EventDto, CreateEventCommand>
{
    public async Task<Result<EventDto, Error>> Handle(
        CreateEventCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        string code = command.Request.Code.Trim().ToLowerInvariant();
        if (await eventRepository.CodeExistsAsync(code, excludeEventId: null, cancellationToken))
        {
            return EventErrors.EventCodeDuplicate;
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

        var eventResult = GameEvent.Create(
            code,
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

        if (eventResult.IsFailure)
        {
            return eventResult.Error;
        }

        await eventRepository.AddAsync(eventResult.Value, cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure
            ? saveResult.Error
            : EventMapper.ToDto(eventResult.Value, DateTime.UtcNow);
    }
}
