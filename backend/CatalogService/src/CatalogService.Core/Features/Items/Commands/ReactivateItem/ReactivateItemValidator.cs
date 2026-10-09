using FluentValidation;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Items.Commands.ReactivateItem;

public sealed class ReactivateItemValidator : AbstractValidator<ReactivateItemCommand>
{
    public ReactivateItemValidator()
    {
        RuleFor(command => command.ItemId)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("itemId"));
    }
}
