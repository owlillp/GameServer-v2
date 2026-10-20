using FluentValidation;
using EventService.Domain;

namespace EventService.Core.Features.Progress.Commands.ClaimReward;

public sealed class ClaimRewardValidator : AbstractValidator<ClaimRewardCommand>
{
    public ClaimRewardValidator()
    {
        RuleFor(command => command.EventCode)
            .NotEmpty()
            .MaximumLength(EventConstants.CODE_MAX_LENGTH);

        RuleFor(command => command.RewardCode)
            .NotEmpty()
            .MaximumLength(EventConstants.CODE_MAX_LENGTH);

        RuleFor(command => command.AccountId)
            .NotEmpty();

        RuleFor(command => command.IdempotencyKey)
            .MaximumLength(EventConstants.IDEMPOTENCY_KEY_MAX_LENGTH);
    }
}
