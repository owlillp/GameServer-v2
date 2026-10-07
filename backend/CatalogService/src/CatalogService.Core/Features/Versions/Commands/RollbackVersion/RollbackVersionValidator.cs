using FluentValidation;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;

namespace CatalogService.Core.Features.Versions.Commands.RollbackVersion;

public sealed class RollbackVersionValidator : AbstractValidator<RollbackVersionCommand>
{
    public RollbackVersionValidator()
    {
        RuleFor(command => command.Request.TargetNumber)
            .GreaterThan(0).WithError(GeneralErrors.ValueIsInvalid("targetNumber"));
    }
}
