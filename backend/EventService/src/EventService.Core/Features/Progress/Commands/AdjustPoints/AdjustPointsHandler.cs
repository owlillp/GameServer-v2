using CSharpFunctionalExtensions;
using FluentValidation;
using EventService.Contracts.Dtos;
using EventService.Core.Abstractions;
using EventService.Core.Features.Events.Models;
using EventService.Domain.Events;
using EventService.Domain.Progress;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Progress.Commands.AdjustPoints;

public sealed class AdjustPointsHandler(
    IValidator<AdjustPointsCommand> validator,
    IEventRepository eventRepository,
    IEventParticipationRepository participationRepository,
    ITransactionManager transactionManager) : ICommandHandler<EventProgressDto, AdjustPointsCommand>
{
    public async Task<Result<EventProgressDto, Error>> Handle(
        AdjustPointsCommand command,
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

        GameEvent gameEvent = eventResult.Value;
        EventProgress progress = await participationRepository.GetOrCreateProgressAsync(
            gameEvent.Id,
            command.AccountId,
            cancellationToken);

        var adjustResult = command.Amount > 0
            ? progress.AddPoints(command.Amount)
            : progress.SpendPoints(-command.Amount);

        if (adjustResult.IsFailure)
        {
            return adjustResult.Error;
        }

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        IReadOnlyList<EventQuestProgress> questProgresses =
            await participationRepository.ListQuestProgressAsync(
                gameEvent.Id,
                command.AccountId,
                cancellationToken);

        return EventMapper.ToProgressDto(gameEvent, progress, questProgresses, command.AccountId, DateTime.UtcNow);
    }
}
