using TemplateService.Domain;
using FluentValidation;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace TemplateService.Core.Features.Items.Commands.UpdateItem;

public sealed class UpdateItemValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemValidator()
    {
        RuleFor(command => command.Request.Name)
            .NotEmpty()
            .WithError(GeneralErrors.ValueIsRequired("name"))
            .MaximumLength(ItemConstants.NAME_MAX_LENGTH)
            .WithError(GeneralErrors.LengthIsInvalid("name", max: ItemConstants.NAME_MAX_LENGTH));

        RuleFor(command => command.Request.Description)
            .MaximumLength(ItemConstants.DESCRIPTION_MAX_LENGTH)
            .WithError(GeneralErrors.LengthIsInvalid("description", max: ItemConstants.DESCRIPTION_MAX_LENGTH));
    }
}
