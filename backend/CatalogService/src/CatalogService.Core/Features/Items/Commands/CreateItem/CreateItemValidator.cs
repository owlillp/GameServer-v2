using CatalogService.Contracts.Requests;
using FluentValidation;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Items.Commands.CreateItem;

public sealed class CreateItemValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemValidator()
    {
        RuleFor(command => command.Request.Code)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("code"));

        RuleFor(command => command.Request.DisplayName)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("displayName"));

        RuleFor(command => command.Request.Kind)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("kind"));

        RuleFor(command => command.Request.Rarity)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("rarity"));
    }
}
