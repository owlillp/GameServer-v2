using FluentValidation;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace InventoryService.Core.Features.Inventories.Commands.GrantItems;

public sealed class GrantItemsValidator : AbstractValidator<GrantItemsCommand>
{
    public GrantItemsValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("accountId"));

        RuleFor(command => command.Request.Source)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("source"));

        RuleFor(command => command.Request.Items)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("items"));

        RuleForEach(command => command.Request.Items).ChildRules(item =>
        {
            item.RuleFor(request => request.ItemId)
                .NotEmpty().WithError(GeneralErrors.ValueIsRequired("itemId"));
            item.RuleFor(request => request.Quantity)
                .GreaterThan(0).WithError(InventoryService.Domain.Inventories.InventoryErrors.QuantityInvalid);
        });
    }
}
