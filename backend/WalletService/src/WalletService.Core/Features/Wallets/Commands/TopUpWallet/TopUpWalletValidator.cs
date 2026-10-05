using FluentValidation;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;
using WalletService.Domain.Wallets;

namespace WalletService.Core.Features.Wallets.Commands.TopUpWallet;

public sealed class TopUpWalletValidator : AbstractValidator<TopUpWalletCommand>
{
    public TopUpWalletValidator()
    {
        RuleFor(command => command.WalletId)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("walletId"));

        RuleFor(command => command.Currency)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("currency"))
            .MaximumLength(Currency.MAX_LENGTH)
            .WithError(GeneralErrors.LengthIsInvalid("currency", max: Currency.MAX_LENGTH));

        RuleFor(command => command.Amount)
            .GreaterThan(0).WithError(WalletErrors.AmountMustBePositive);

        RuleFor(command => command.Reason)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("reason"));
    }
}
