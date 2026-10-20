using FluentValidation;

namespace StoreService.Core.Features.Orders.Commands.PurchaseOffer;

public sealed class PurchaseOfferValidator : AbstractValidator<PurchaseOfferCommand>
{
    public PurchaseOfferValidator()
    {
        RuleFor(command => command.AccountId).NotEmpty();
        RuleFor(command => command.Request.OfferId).NotEmpty();
    }
}
