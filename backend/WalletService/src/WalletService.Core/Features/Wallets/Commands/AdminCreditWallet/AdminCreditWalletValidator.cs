using FluentValidation;
using Shared.Core.Validation;
using Shared.SharedKernel.Errors;
using WalletService.Domain.Wallets;

namespace WalletService.Core.Features.Wallets.Commands.AdminCreditWallet;

public sealed class AdminCreditWalletValidator : AbstractValidator<AdminCreditWalletCommand>
{
    public AdminCreditWalletValidator()
    {
        RuleFor(command => command.WalletId)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("walletId"));

        RuleFor(command => command.Request.Currency)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("currency"))
            .MaximumLength(Currency.MAX_LENGTH)
            .WithError(GeneralErrors.LengthIsInvalid("currency", max: Currency.MAX_LENGTH));

        RuleFor(command => command.Request.Amount)
            .GreaterThan(0).WithError(WalletErrors.AmountMustBePositive);

        RuleFor(command => command.Request.Reason)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("reason"));

        RuleFor(command => command.Request.Comment)
            .NotEmpty().WithError(GeneralErrors.ValueIsRequired("comment"))
            .MaximumLength(WalletConstants.COMMENT_MAX_LENGTH)
            .WithError(GeneralErrors.LengthIsInvalid("comment", max: WalletConstants.COMMENT_MAX_LENGTH));
    }
}
