using FluentValidation;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Items.Commands.UpdateItem;

public sealed class UpdateItemValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemValidator()
    {
        RuleFor(command => command.ItemId)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("itemId"));

        RuleFor(command => command.Request.DisplayName)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("displayName"));

        RuleFor(command => command.Request.Rarity)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("rarity"));
    }
}
