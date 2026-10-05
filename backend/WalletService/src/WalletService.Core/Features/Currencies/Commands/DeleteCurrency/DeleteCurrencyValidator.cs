using FluentValidation;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;
using WalletService.Domain.Wallets;

namespace WalletService.Core.Features.Currencies.Commands.DeleteCurrency;

public sealed class DeleteCurrencyValidator : AbstractValidator<DeleteCurrencyCommand>
{
    public DeleteCurrencyValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("code"))
            .MaximumLength(Currency.MAX_LENGTH)
            .WithError(GeneralErrors.LengthIsInvalid("code", max: Currency.MAX_LENGTH));
    }
}
