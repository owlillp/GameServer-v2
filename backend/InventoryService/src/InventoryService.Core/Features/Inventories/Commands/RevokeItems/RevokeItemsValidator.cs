using FluentValidation;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Features.Inventories.Commands.RevokeItems;

public sealed class RevokeItemsValidator : AbstractValidator<RevokeItemsCommand>
{
    public RevokeItemsValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("accountId"));

        RuleFor(command => command.Request.ItemIds)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("itemIds"));
    }
}
