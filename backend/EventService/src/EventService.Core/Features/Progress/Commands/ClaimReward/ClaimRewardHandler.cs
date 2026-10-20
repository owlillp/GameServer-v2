using CSharpFunctionalExtensions;
using FluentValidation;
using EventService.Contracts.Dtos;
using EventService.Core.Abstractions;
using EventService.Core.Features.Events.Models;
using EventService.Core.Features.Progress.Commands.ClaimReward;
using EventService.Core.Messaging.Publishers;
using EventService.Domain;
using EventService.Domain.Events;
using EventService.Domain.Progress;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace EventService.Core.Features.Progress.Commands.ClaimReward;

public sealed class ClaimRewardHandler(
    IValidator<ClaimRewardCommand> validator,
    IEventRepository eventRepository,
    IEventParticipationRepository participationRepository,
    IEventsEventPublisher eventPublisher,
    ITransactionManager transactionManager) : ICommandHandler<EventRewardClaimDto, ClaimRewardCommand>
{
    public async Task<Result<EventRewardClaimDto, Error>> Handle(
        ClaimRewardCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var eventResult = await eventRepository.GetByCodeAsync(
            command.EventCode,
            includeDetails: true,
            cancellationToken);

        if (eventResult.IsFailure)
        {
            return eventResult.Error;
        }

        GameEvent gameEvent = eventResult.Value;

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            EventRewardClaim? existing = await participationRepository.FindClaimByIdempotencyKeyAsync(
                command.AccountId,
                command.IdempotencyKey.Trim(),
                cancellationToken);

            if (existing is not null)
            {
                return EventMapper.ToClaimDto(existing);
            }
        }

        if (!gameEvent.IsAcceptingClaims(DateTime.UtcNow))
        {
            return EventErrors.EventNotAcceptingClaims;
        }

        EventReward? reward = gameEvent.FindRewardByCode(command.RewardCode);
        if (reward is null)
        {
            return EventErrors.RewardNotFound;
        }

        if (!reward.IsRepeatable
            && await participationRepository.HasClaimAsync(command.AccountId, reward.Id, cancellationToken))
        {
            return EventErrors.RewardAlreadyClaimed;
        }

        EventProgress progress = await participationRepository.GetOrCreateProgressAsync(
            gameEvent.Id,
            command.AccountId,
            cancellationToken);

        var spendResult = progress.SpendPoints(reward.PointsCost);
        if (spendResult.IsFailure)
        {
            return spendResult.Error;
        }

        var claimResult = EventRewardClaim.Create(
            gameEvent.Id,
            reward.Id,
            reward.Code,
            command.AccountId,
            reward.PointsCost,
            reward.IsRepeatable,
            command.IdempotencyKey);

        if (claimResult.IsFailure)
        {
            return claimResult.Error;
        }

        EventRewardClaim claim = claimResult.Value;
        await participationRepository.AddClaimAsync(claim, cancellationToken);

        await eventPublisher.PublishGrantRewardAsync(
            claim.Id,
            gameEvent.Id,
            reward,
            command.AccountId,
            cancellationToken);

        var saveResult = await transactionManager.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure
            ? saveResult.Error
            : EventMapper.ToClaimDto(claim);
    }
}
