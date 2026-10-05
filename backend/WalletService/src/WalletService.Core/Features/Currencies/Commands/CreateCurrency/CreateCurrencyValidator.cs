using FluentValidation;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;
using WalletService.Domain.Wallets;

namespace WalletService.Core.Features.Currencies.Commands.CreateCurrency;

public sealed class CreateCurrencyValidator : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyValidator()
    {
        RuleFor(command => command.Request.Code)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("code"))
            .MaximumLength(Currency.MAX_LENGTH)
            .WithError(GeneralErrors.LengthIsInvalid("code", max: Currency.MAX_LENGTH));

        RuleFor(command => command.Request.DisplayName)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("displayName"))
            .MaximumLength(WalletConstants.CURRENCY_DISPLAY_NAME_MAX_LENGTH)
            .WithError(GeneralErrors.LengthIsInvalid(
                "displayName",
                max: WalletConstants.CURRENCY_DISPLAY_NAME_MAX_LENGTH));

        RuleFor(command => command.Request.Kind)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("kind"))
            .Must(kind => Enum.TryParse(kind, ignoreCase: true, out CurrencyKind parsed) && Enum.IsDefined(parsed))
            .WithError(GeneralErrors.ValueIsInvalid("kind"));
    }
}
