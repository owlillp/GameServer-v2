using FluentValidation;
using EventService.Domain;

namespace EventService.Core.Features.Events.Commands.UpdateEvent;

public sealed class UpdateEventValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.Request.DisplayName)
            .NotEmpty()
            .MaximumLength(EventConstants.DISPLAY_NAME_MAX_LENGTH);

        RuleFor(command => command.Request.Description)
            .MaximumLength(EventConstants.DESCRIPTION_MAX_LENGTH);

        RuleFor(command => command.Request.Kind)
            .NotEmpty();

        RuleFor(command => command.Request.CurrencyCode)
            .NotEmpty()
            .MaximumLength(EventConstants.CURRENCY_MAX_LENGTH);

        RuleFor(command => command.Request.EndsAt)
            .GreaterThan(command => command.Request.StartsAt);

        RuleFor(command => command.Request.AnnounceAt)
            .LessThanOrEqualTo(command => command.Request.StartsAt);

        RuleFor(command => command.Request.GraceEndsAt)
            .GreaterThanOrEqualTo(command => command.Request.EndsAt);

        RuleFor(command => command.Request.Quests)
            .NotNull()
            .Must(quests => quests.Count <= EventConstants.MAX_QUESTS);

        RuleForEach(command => command.Request.Quests).ChildRules(quest =>
        {
            quest.RuleFor(request => request.Code)
                .NotEmpty()
                .MaximumLength(EventConstants.CODE_MAX_LENGTH);

            quest.RuleFor(request => request.DisplayName)
                .NotEmpty()
                .MaximumLength(EventConstants.DISPLAY_NAME_MAX_LENGTH);

            quest.RuleFor(request => request.Description)
                .MaximumLength(EventConstants.DESCRIPTION_MAX_LENGTH);

            quest.RuleFor(request => request.Kind).NotEmpty();
            quest.RuleFor(request => request.Metric).NotEmpty();
            quest.RuleFor(request => request.Target).GreaterThan(0);
            quest.RuleFor(request => request.Points).GreaterThan(0);
        });

        RuleFor(command => command.Request.Rewards)
            .NotNull()
            .Must(rewards => rewards.Count <= EventConstants.MAX_REWARDS);

        RuleForEach(command => command.Request.Rewards).ChildRules(reward =>
        {
            reward.RuleFor(request => request.Code)
                .NotEmpty()
                .MaximumLength(EventConstants.CODE_MAX_LENGTH);

            reward.RuleFor(request => request.DisplayName)
                .NotEmpty()
                .MaximumLength(EventConstants.DISPLAY_NAME_MAX_LENGTH);

            reward.RuleFor(request => request.Description)
                .MaximumLength(EventConstants.DESCRIPTION_MAX_LENGTH);

            reward.RuleFor(request => request.PointsCost).GreaterThan(0);
            reward.RuleFor(request => request.CatalogItemId).NotEmpty();
            reward.RuleFor(request => request.ItemCode)
                .NotEmpty()
                .MaximumLength(EventConstants.ITEM_CODE_MAX_LENGTH);

            reward.RuleFor(request => request.ItemKind)
                .NotEmpty()
                .MaximumLength(EventConstants.ITEM_KIND_MAX_LENGTH);

            reward.RuleFor(request => request.Slot)
                .MaximumLength(EventConstants.SLOT_MAX_LENGTH);

            reward.RuleFor(request => request.Quantity).GreaterThan(0);
        });
    }
}
