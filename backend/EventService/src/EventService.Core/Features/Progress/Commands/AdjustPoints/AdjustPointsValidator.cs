using FluentValidation;

namespace EventService.Core.Features.Progress.Commands.AdjustPoints;

public sealed class AdjustPointsValidator : AbstractValidator<AdjustPointsCommand>
{
    public AdjustPointsValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.AccountId).NotEmpty();
        RuleFor(command => command.Amount).NotEqual(0);
    }
}
