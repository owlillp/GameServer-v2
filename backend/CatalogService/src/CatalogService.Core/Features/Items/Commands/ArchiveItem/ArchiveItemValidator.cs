using FluentValidation;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Items.Commands.ArchiveItem;

public sealed class ArchiveItemValidator : AbstractValidator<ArchiveItemCommand>
{
    public ArchiveItemValidator()
    {
        RuleFor(command => command.ItemId)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("itemId"));
    }
}
